namespace Bluepath.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Services;

    public abstract class DistributedThread
    {
        private readonly IConnectionManager connectionManager;

        protected DistributedThread(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public enum ExecutorSelectionMode : int
        {
            LocalOnly = 0,
            RemoteOnly = 1
        }

        public List<ServiceReferences.IRemoteExecutorService> RemoteServices
        {
            get
            {
                return this.connectionManager.RemoteServices;
            }
        }

        public ExecutorSelectionMode Mode { get; protected set; }

        public ExecutorState State
        {
            get
            {
                return this.Executor.ExecutorState;
            }
        }

        public object Result
        {
            get
            {
                return this.Executor.Result;
            }
        }

        protected IExecutor Executor { get; set; }

        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, mode);
        }

        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, IConnectionManager connectionManager, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, connectionManager, mode);
        }

        public abstract void Start(object[] parameters);

        public void Join()
        {
            this.Executor.Join();
        }
    }

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    /// <typeparam name="TFunc">
    /// Delegate type of method to be run.
    /// </typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
    public class DistributedThread<TFunc> : DistributedThread
    {
        private TFunc function;

        protected DistributedThread()
            : base(ConnectionManager.Default)
        {
        }

        protected DistributedThread(IConnectionManager connectionManager)
            : base(connectionManager)
        {
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

        public override void Start(object[] parameters)
        {
            switch (this.Mode)
            {
                case DistributedThread.ExecutorSelectionMode.LocalOnly:
                    var localExecutor = new LocalExecutor();
                    localExecutor.Initialize(this.function);
                    this.Executor = localExecutor;
                    break;
                case DistributedThread.ExecutorSelectionMode.RemoteOnly:
                    var remoteExecutor = new RemoteExecutor();
                    var service = this.RemoteServices.FirstOrDefault();
                    if (service == null)
                    {
                        throw new NullReferenceException("No remote service was specified in DistributedThread.RemoteServices!");
                    }

                    remoteExecutor.Setup(service, BluepathSingleton.Instance.CallbackUri.Convert());
                    remoteExecutor.Initialize(this.function);
                    RemoteExecutorService.RegisterRemoteExecutor(remoteExecutor);
                    this.Executor = remoteExecutor;
                    break;
            }

            this.Executor.Execute(parameters);
        }
    }
}
