﻿namespace Bluepath.Executor
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Threading;
    using System.Threading.Tasks;

    using Bluepath.Exceptions;

    using global::Bluepath.Extensions;

    using global::Bluepath.ServiceReferences;

    public class RemoteExecutor : Executor, IRemoteExecutor
    {
        private readonly object executorStateLock = new object();
        private readonly object joinThreadLock = new object();
        private readonly object waitForCallbackLock = new object();
        private readonly TimeSpan repeatedTryJoinDelayTime = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 1, milliseconds: 0);
        private RemoteExecutorServiceResult callbackResult;
        private object result;
        private Thread joinThread;
        private ServiceUri callbackUri;
        private bool callbacksEnabled = true;

        public RemoteExecutor()
        {
            this.ExecutorState = ExecutorState.NotStarted;
        }

        public override TimeSpan? ElapsedTime { get; protected set; }

        public override object Result
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

        public override async void Execute(object[] parameters)
        {
            lock (this.executorStateLock)
            {
                this.ExecutorState = ExecutorState.Running;
            }

            await this.Client.ExecuteAsync(this.Eid, parameters, this.callbackUri);
        }

        /// <summary>
        /// Call Join on remote executor and get result if available. This method is blocking.
        /// </summary>
        /// <exception cref="RemoteException">Rethrows exception that occurred on the remote executor.</exception>
        /// <exception cref="RemoteJoinAbortedException">Thrown if join thread ends unexpectedly (eg. endpoint was not found).</exception>
        public override void Join()
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
                            if (this.callbacksEnabled)
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
                                    Log.TraceMessage(string.Format("Remote TryJoinAsync timed out for {0} time. Trying again...", attemptsCounter), Log.MessageType.Trace, this.Eid.EidAsLogKeywords());
                                }
                                catch (Exception ex)
                                {
                                    joinThreadException = ex;
                                    Log.TraceMessage(string.Format("Executor failed on remote TryJoinAsync with exception '{0}'. RemoteJoinAbortedException will be thrown with this exception inside.", ex.Message), Log.MessageType.Trace, this.Eid.EidAsLogKeywords());

                                    break;
                                }

                                if (joinResult != null && joinResult.ExecutorState == ServiceReferences.ExecutorState.Running)
                                {
                                    // TryJoin is non-blocking, wait some time before checking again.
                                    Thread.Sleep(this.repeatedTryJoinDelayTime);
                                }
                            }
                            while (joinResult == null || joinResult.ExecutorState == ServiceReferences.ExecutorState.Running);

                            this.CleanUpJoinThread();
                        });

                    this.joinThread.Name = string.Format("Join thread on remote executor '{0}'", this.Eid);
                    this.joinThread.Start();
                }
            }

            this.joinThread.Join();

            if (joinResult == null)
            {
                this.ExecutorState = ExecutorState.Faulted;
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

        public Task JoinAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            var t = new Thread(
                () =>
                {
                    try
                    {
                        this.Join();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

            t.Name = string.Format("JoinAsync thread on remote executor '{0}'", this.Eid);
            t.Start();

            return tcs.Task;
        }

        /// <summary>
        /// Invoked after receiving remote callback with 'processing finished' message.
        /// </summary>
        /// <param name="result">
        /// Result that came along with callback.
        /// </param>
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

        public override void Dispose()
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

        public void Setup(IRemoteExecutorService remoteExecutorService, ServiceUri callbackUri)
        {
            this.Initialize(remoteExecutorService);
            if (callbackUri != null)
            {
                this.callbackUri = callbackUri;
                this.callbacksEnabled = true;
            }
            else
            {
                this.callbacksEnabled = false;
            }
        }

        /// <summary>
        /// This method is invoked before calling Initialize on remote executor.
        /// </summary>
        /// <param name="remoteExecutorService">
        /// Client for remote executor service.
        /// </param>
        protected virtual void Initialize(IRemoteExecutorService remoteExecutorService)
        {
            this.Client = remoteExecutorService;
        }

        protected override void InitializeFromMethod(MethodBase method)
        {
            if (!method.IsStatic)
            {
                throw new ArgumentException("Remote executor supports only static methods.", "method");
            }

            this.Eid = this.Client.Initialize(method.SerializeMethodHandle());
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
