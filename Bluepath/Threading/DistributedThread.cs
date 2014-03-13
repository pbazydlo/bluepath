namespace Bluepath.Threading
{
    using System;
    using System.Linq;
    using global::Bluepath.Executor;
    using System.Collections.Generic;

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    public class DistributedThread
    {
        private IExecutor executor;

        private Func<object[], object> function;

        private DistributedThread() { }

        public static readonly List<ServiceReferences.IRemoteExecutorService> RemoteServices = new List<ServiceReferences.IRemoteExecutorService>();

        public static DistributedThread Create(Func<object[], object> function, ExecutorSelectionMode mode = ExecutorSelectionMode.LocalOnly)
        {
            return new DistributedThread()
            {
                function = function,
                Mode = mode
            };
        }

        public ExecutorSelectionMode Mode { get; private set; }

        public void Start(object[] parameters)
        {
            switch (this.Mode)
            {
                case ExecutorSelectionMode.LocalOnly:
                    var localExecutor = new LocalExecutor();
                    localExecutor.Initialize(this.function);
                    this.executor = localExecutor;
                    break;
                case ExecutorSelectionMode.RemoteOnly:
                    var remoteExecutor = new RemoteExecutor();
                    var service = RemoteServices.FirstOrDefault();
                    if(service == null)
                    {
                        throw new NullReferenceException("No remote service was specified in DistributedThread.RemoteServices!");
                    }

                    remoteExecutor.Initialize(service, this.function);
                    this.executor = remoteExecutor;
                    break;
            }

            this.executor.Execute(parameters);
        }

        public void Join()
        {
            this.executor.Join();
        }

        public object Result
        {
            get
            {
                return this.executor.Result;
            }
        }

        public enum ExecutorSelectionMode : int
        {
            LocalOnly = 0,
            RemoteOnly = 1
        }
    }
}
