namespace Bluepath.Executor
{
    using System;

    public class RemoteExecutor : IExecutor, IDisposable
    {
        private Services.ExecutorClient client;
        private object result;
        private bool finishedRunning;
        private object finishedRunningLock = new object();

        public RemoteExecutor(Func<object[], object> function)
        {
            this.client = new Services.ExecutorClient();
            // TODO: this.client.Initialize(serializedMethodHandle);
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
            this.result = this.client.GetResultAsync().Result;
            return this.result;
        }

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
    }
}
