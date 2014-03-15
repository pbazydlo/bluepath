namespace Bluepath.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Services;

    public abstract class DistributedThread
    {
        public enum ExecutorSelectionMode : int
        {
            LocalOnly = 0,
            RemoteOnly = 1
        }

        private readonly IConnectionManager connectionManager;

        protected DistributedThread(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public List<ServiceReferences.IRemoteExecutorService> RemoteServices
        {
            get
            {
                return this.connectionManager.RemoteServices;
            }
        }

        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, mode);
        }

        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, IConnectionManager connectionManager, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, connectionManager, mode);
        }
    }

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    public class DistributedThread<TFunc> : DistributedThread
    {
        private IExecutor executor;

        private TFunc function;

        protected DistributedThread()
            : base(ConnectionManager.Default)
        {
        }

        protected DistributedThread(IConnectionManager connectionManager)
            : base(connectionManager)
        {
        }

        public DistributedThread.ExecutorSelectionMode Mode { get; private set; }

        public ExecutorState State
        {
            get
            {
                return this.executor.ExecutorState;
            }
        }

        public object Result
        {
            get
            {
                return this.executor.Result;
            }
        }

        public static DistributedThread<TFunc> Create(TFunc function, DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOnly)
        {
            return new DistributedThread<TFunc>()
            {
                function = function,
                Mode = mode
            };
        }

        public static DistributedThread<TFunc> Create(TFunc function, IConnectionManager connectionManager, DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOnly)
        {
            return new DistributedThread<TFunc>(connectionManager)
            {
                function = function,
                Mode = mode
            };
        }

        public void Start(object[] parameters)
        {
            switch (this.Mode)
            {
                case DistributedThread.ExecutorSelectionMode.LocalOnly:
                    var localExecutor = new LocalExecutor();
                    localExecutor.Initialize<TFunc>(this.function);
                    this.executor = localExecutor;
                    break;
                case DistributedThread.ExecutorSelectionMode.RemoteOnly:
                    var remoteExecutor = new RemoteExecutor();
                    var service = this.RemoteServices.FirstOrDefault();
                    if (service == null)
                    {
                        throw new NullReferenceException("No remote service was specified in DistributedThread.RemoteServices!");
                    }

                    remoteExecutor.Setup(service, BluepathSingleton.Instance.CallbackUri.Convert());
                    remoteExecutor.Initialize<TFunc>(this.function);
                    Bluepath.Services.RemoteExecutorService.RegisterRemoteExecutor(remoteExecutor);
                    this.executor = remoteExecutor;
                    break;
            }

            this.executor.Execute(parameters);
        }

        public void Join()
        {
            this.executor.Join();
        }
    }
}
