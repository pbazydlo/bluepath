﻿namespace Bluepath.Executor
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Threading;

    using Bluepath.Exceptions;

    using global::Bluepath.Extensions;

    using global::Bluepath.ServiceReferences;

    public class RemoteExecutor : IRemoteExecutor
    {
        public bool CallbacksEnabled = true;

        private readonly object executorStateLock = new object();
        private readonly object joinThreadLock = new object();
        private readonly object waitForCallbackLock = new object();
        private readonly TimeSpan repeatedTryJoinDelayTime = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 1, milliseconds: 0);
        private RemoteExecutorServiceResult callbackResult;
        private object result;
        private Thread joinThread;

        public RemoteExecutor()
        {
            this.ExecutorState = ExecutorState.NotStarted;
        }

        public Guid Eid { get; private set; }

        public ExecutorState ExecutorState { get; private set; }

        public TimeSpan? ElapsedTime { get; private set; }

        public object Result
        {
            get
            {
                lock (this.executorStateLock)
                {
                    if (this.ExecutorState == ExecutorState.Finished)
                    {
                        return this.result;
                    }

                    throw new NullReferenceException(string.Format("Result is not available. The executor is in '{0}' state.", this.ExecutorState));
                }
            }
        }

        protected Bluepath.ServiceReferences.IRemoteExecutorService Client { get; set; }

        public async void Execute(object[] parameters)
        {
            lock (this.executorStateLock)
            {
                this.ExecutorState = ExecutorState.Running;
            }

            // TODO: pass actual callback Uri instead of null
            await this.Client.ExecuteAsync(this.Eid, parameters, null);
        }

        /// <summary>
        /// Call Join on remote executor and get result if available. This method is blocking.
        /// TODO: async, return Task?, callback with result instead of calling TryJoin?
        /// </summary>
        /// <exception cref="RemoteException">Rethrows exception that occured on the remote executor.</exception>
        /// <exception cref="RemoteJoinAbortedException">Thrown if join thread ends unexpectedly (eg. endpoint was not found).</exception>
        public void Join()
        {
            lock (this.executorStateLock)
            {
                if (this.ExecutorState != ExecutorState.Running)
                {
                    return;
                }
            }

            var joinThreadException = default(Exception);
            var joinResult = default(ServiceReferences.RemoteExecutorServiceResult);

            lock (this.joinThreadLock)
            {
                if (this.joinThread == null)
                {
                    this.joinThread = new Thread(
                        () =>
                        {
                            if (this.CallbacksEnabled)
                            {
                                lock (this.waitForCallbackLock)
                                {
                                    // Check for buffered callbacks that we may have missed before RemoteExecutor.Join was called
                                    if (this.callbackResult == null)
                                    {
                                        // TODO: What if remote executor fails and we never receive callback
                                        // (not necessarily to be solved here, maybe in 'node discovery' layer)

                                        // Block on synchronization object and wait for pulse generated by callback
                                        Monitor.Wait(this.waitForCallbackLock);
                                    }

                                    this.CleanUpJoinThread();
                                    joinResult = this.callbackResult;
                                    return;
                                }
                            }

                            var attemptsCounter = 0;
                            
                            // Get the processing result
                            // I would leave this loop to allow testing without communication (callbacks)
                            do
                            {
                                attemptsCounter++;

                                try
                                {
                                    joinResult = this.Client.TryJoin(this.Eid);
                                }
                                catch (TimeoutException)
                                {
                                    Log.TraceMessage(string.Format("Remote TryJoinAsync timed out for {0} time. Trying again...", attemptsCounter), Log.MessageType.Trace, this.Eid.AsLogKeywords("eid"));
                                }
                                catch (Exception ex)
                                {
                                    joinThreadException = ex;
                                    Log.TraceMessage(string.Format("Executor failed on remote TryJoinAsync with exception '{0}'. RemoteJoinAbortedException will be thrown with this exception inside.", ex.Message), Log.MessageType.Trace, this.Eid.AsLogKeywords("eid"));
                                    
                                    break;
                                }

                                if (joinResult != null && joinResult.ExecutorState == ServiceReferences.ExecutorState.Running)
                                {
                                    // TryJoin is non-blocking, wait some time before checking again.
                                    Thread.Sleep(repeatedTryJoinDelayTime);
                                }
                            }
                            while (joinResult == null || joinResult.ExecutorState == ServiceReferences.ExecutorState.Running);

                            this.CleanUpJoinThread();
                        });

                    this.joinThread.Start();
                }
            }

            this.joinThread.Join();

            if (joinResult == null)
            {
                throw new RemoteJoinAbortedException(
                    "Remote thread awaiter has joined but the result is not available. See inner exception for details.",
                    joinThreadException);
            }

            switch (joinResult.ExecutorState)
            {
                case ServiceReferences.ExecutorState.Finished:
                    lock (this.executorStateLock)
                    {
                        if (this.ExecutorState == ExecutorState.Running)
                        {
                            this.ExecutorState = ExecutorState.Finished;
                            this.ElapsedTime = joinResult.ElapsedTime;
                            this.result = joinResult.Result;
                        }
                    }

                    break;
                case ServiceReferences.ExecutorState.Faulted:
                    lock (this.executorStateLock)
                    {
                        this.ExecutorState = ExecutorState.Faulted;
                    }

                    throw new RemoteException("Exception was thrown on the remote executor. See inner exception for details.", joinResult.Error);
            }
        }

        /// <summary>
        /// Invoked after receiving remote callback with 'processing finished' message.
        /// </summary>
        public void Pulse(RemoteExecutorServiceResult result)
        {
            lock (this.waitForCallbackLock)
            {
                // TODO: what do we do in case of second (possibly repeated due to network fault) Pulse?
                // if(this.callbackResult != null)
                // {
                // }

                // Store callback for retrieval
                this.callbackResult = result;

                // Wake up Join which was called before Pulse
                Monitor.PulseAll(this.waitForCallbackLock);
            }
        }

        public object GetResult()
        {
            return this.Result;
        }

        public void Dispose()
        {
            if (this.Client is System.ServiceModel.ClientBase<IRemoteExecutorService>)
            {
                (this.Client as System.ServiceModel.ClientBase<IRemoteExecutorService>).Close();
            }

            if (this.Client is IDisposable)
            {
                (this.Client as IDisposable).Dispose();
            }
        }

        #region Generic Initialize overloads
        public void Initialize<TResult>(IRemoteExecutorService remoteExecutorService, Func<TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, T3, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, T3, T4, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, T7, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            this.Initialize(remoteExecutorService, function.Method);
        }

        #endregion

        /// <summary>
        /// This method is invoked before calling Initialize on remote executor.
        /// </summary>
        protected virtual void Initialize(IRemoteExecutorService remoteExecutorService)
        {
            this.Client = remoteExecutorService;
        }

        protected async void Initialize(IRemoteExecutorService remoteExecutorService, MethodInfo method)
        {
            if (!method.IsStatic)
            {
                throw new ArgumentException("Remote executor supports only static methods.", "method");
            }

            this.Initialize(remoteExecutorService);
            // TODO: pass callback URI (here or on 'Execute') and set this.callbacksEnabled to true
            this.Eid = await this.Client.InitializeAsync(method.SerializeMethodHandle());
        }

        private void CleanUpJoinThread()
        {
            lock (this.joinThreadLock)
            {
                this.joinThread = null;
            }
        }
    }
}
