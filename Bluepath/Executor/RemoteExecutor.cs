namespace Bluepath.Executor
{
    using System;

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
            await this.client.ExecuteAsync(parameters);
        }

        // TODO: Assign 'true' to this.finishedRunning somewhere (after callback?)
        public async void Join()
        {
            await this.client.JoinAsync();
        }

        // TODO: async?, callback with result?
        public object GetResult()
        {
            lock (this.finishedRunningLock)
            {
                if (!this.finishedRunning)
                {
                    this.result = this.client.GetResultAsync().Result;
                    this.finishedRunning = true;
                }
            }

            return this.result;
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

                    throw new NullReferenceException("Cannot fetch results before starting and finishing Execute.");
                }
            }
        }

        public void Dispose()
        {
            this.client.Close();
            ((IDisposable)this.client).Dispose();
        }

        #region Initialize
        public void Initialize(Func<object[], object> function)
        {
            this.Initialize();
            this.client.Initialize(function.GetSerializedMethodHandle());
        }

        public void Initialize<TResult>(Func<TResult> function)
        {
            this.Initialize();
            this.client.Initialize(function.GetSerializedMethodHandle());
        }

        public void Initialize<T1, TResult>(Func<T1, TResult> function)
        {
            this.Initialize();
            this.client.Initialize(function.GetSerializedMethodHandle());
        }

        public void Initialize<T1, T2, TResult>(Func<T1, T2, TResult> function)
        {
            this.Initialize();
            this.client.Initialize(function.GetSerializedMethodHandle());
        }
        #endregion

        private void Initialize()
        {
            this.client = new RemoteExecutorServiceClient();
        }
    }
}
