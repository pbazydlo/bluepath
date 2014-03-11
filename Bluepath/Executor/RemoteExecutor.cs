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
        private readonly object executorStateLock = new object();
        private readonly object joinThreadLock = new object();
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

            await this.Client.ExecuteAsync(this.Eid, parameters);
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
                        async () =>
                        {
                            // wait for remote join
                            do
                            {
                                try
                                {
                                    joinResult = await this.Client.TryJoinAsync(this.Eid);
                                }
                                catch (TimeoutException)
                                {
                                }
                                catch (Exception ex)
                                {
                                    joinThreadException = ex;
                                    break;
                                }

                                if (joinResult != null && joinResult.ExecutorState == ServiceReferences.ExecutorState.Running)
                                {
                                    // if TryJoin is non-blocking, wait some time before checking again.
                                    // TODO: remote TryJoin should block and throw TimeoutException
                                    Thread.Sleep(1000);
                                }
                            }
                            while (joinResult == null || joinResult.ExecutorState == ServiceReferences.ExecutorState.Running);

                            lock (this.joinThreadLock)
                            {
                                this.joinThread = null;
                            }
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
        public void Initialize(Func<object[], object> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<TResult>(Func<TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, TResult>(Func<T1, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, TResult>(Func<T1, T2, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            this.Initialize(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            this.Initialize(function.Method);
        }

        #endregion

        /// <summary>
        /// This method is invoked before calling Initialize on remote executor.
        /// </summary>
        protected virtual void Initialize()
        {
            this.Client = new RemoteExecutorServiceClient();
        }

        protected async void Initialize(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                throw new ArgumentException("Remote executor supports only static methods.", "method");
            }

            this.Initialize();
            this.Eid = await this.Client.InitializeAsync(method.SerializeMethodHandle());
        }
    }
}
