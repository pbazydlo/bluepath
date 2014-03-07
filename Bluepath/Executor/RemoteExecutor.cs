namespace Bluepath.Executor
{
    using ServiceReferences;
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class RemoteExecutor : IExecutor, IDisposable
    {
        private object result;
        private bool finishedRunning;
        private object finishedRunningLock = new object();
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

        public void Initialize(Func<object[], object> function)
        {
            this.client = new RemoteExecutorServiceClient();
            BinaryFormatter frm = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                frm.Serialize(stream, function.Method.MethodHandle);
                stream.Seek(0, SeekOrigin.Begin);
                var serializedMethodHandle = stream.GetBuffer();
                this.client.Initialize(serializedMethodHandle);
            }
        }
    }
}
