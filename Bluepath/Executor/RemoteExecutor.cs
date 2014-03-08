namespace Bluepath.Executor
{
    using System;
    using System.Runtime.Remoting;

    using global::Bluepath.Extensions;

    using global::Bluepath.ServiceReferences;

    public class RemoteExecutor : IRemoteExecutor
    {
        private readonly object finishedRunningLock = new object();
        private object result;
        private bool finishedRunning;
        private RemoteExecutorServiceClient client;

        public RemoteExecutor()
        {
        }

        public async void Execute(object[] parameters)
        {
            await this.client.ExecuteAsync(this.Eid, parameters);
        }

        /// <summary>
        /// Call Join on remote executor and get result if available.
        /// NOTE: This method is non-blocking. Check manually if execution has finished.
        /// TODO: async?, callback with result? make this method blocking.
        /// </summary>
        /// <exception cref="RemotingException">Rethrows exception that occured on the remote executor.</exception>
        public async void Join()
        {
            lock (this.finishedRunningLock)
            {
                if (this.finishedRunning)
                {
                    return;
                }
            }

            var joinResult = await this.client.TryJoinAsync(this.Eid);
            switch (joinResult.ExecutorState)
            {
                case RemoteExecutorServiceResult.State.Finished:
                    lock (this.finishedRunningLock)
                    {
                        if (!this.finishedRunning)
                        {
                            this.finishedRunning = true;
                            this.result = joinResult.Result;
                        }
                    }

                    break;
                case RemoteExecutorServiceResult.State.Faulted:
                    throw new RemotingException("Exception was thrown on remote executor. See inner exception for details.", joinResult.Error);
                    break;
            }
        }

        public object GetResult()
        {
            return this.Result;
        }

        public Guid Eid { get; private set; }

        public object Result
        {
            get
            {
                lock (this.finishedRunningLock)
                {
                    if (this.finishedRunning)
                    {
                        return this.result;
                    }

                    throw new NullReferenceException("Result is not available. The executor is still running.");
                }
            }
        }

        public void Dispose()
        {
            this.client.Close();
            ((IDisposable)this.client).Dispose();
        }

        #region Call remote Initialize method
        public async void Initialize(Func<object[], object> function)
        {
            this.Initialize();
            this.Eid = await this.client.InitializeAsync(function.GetSerializedMethodHandle());
        }

        public async void Initialize<TResult>(Func<TResult> function)
        {
            this.Initialize();
            this.Eid = await this.client.InitializeAsync(function.GetSerializedMethodHandle());
        }

        public async void Initialize<T1, TResult>(Func<T1, TResult> function)
        {
            this.Initialize();
            this.Eid = await this.client.InitializeAsync(function.GetSerializedMethodHandle());
        }

        public async void Initialize<T1, T2, TResult>(Func<T1, T2, TResult> function)
        {
            this.Initialize();
            this.Eid = await this.client.InitializeAsync(function.GetSerializedMethodHandle());
        }
        #endregion

        /// <summary>
        /// This method is invoked before calling Initialize on remote executor.
        /// </summary>
        private void Initialize()
        {
            this.client = new RemoteExecutorServiceClient();
        }
    }
}
