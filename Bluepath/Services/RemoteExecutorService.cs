namespace Bluepath.Services
{
    using System;
    using System.Threading;

    /// <summary>
    /// Represents endpoint, runs thread using local executor on the remote machine
    /// </summary>
    public class RemoteExecutorService : IRemoteExecutorService
    {
        private IExecutor executor;

        private Func<object[], object> function;

        public void Initialize(byte[] methodHandle)
        {
            // find method using serialized RuntimeMethodHandle
            throw new NotImplementedException();
        }

        public void Execute(object[] parameters)
        {
            // run function on LocalExecutor
            throw new NotImplementedException();
        }

        public void Join()
        {
            throw new NotImplementedException();
        }

        public object GetResult()
        {
            return this.Result;
        }

        public object Result
        {
            get { throw new NotImplementedException(); }
        }
    }
}
