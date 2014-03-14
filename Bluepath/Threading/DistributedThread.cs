namespace Bluepath.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Bluepath.Executor;
    using Bluepath.Extensions;

    public class DistributedThread
    {
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static readonly List<ServiceReferences.IRemoteExecutorService> remoteServices = new List<ServiceReferences.IRemoteExecutorService>();

        private DistributedThread()
        {
        }

        public enum ExecutorSelectionMode : int
        {
            LocalOnly = 0,
            RemoteOnly = 1
        }

        public static List<ServiceReferences.IRemoteExecutorService> RemoteServices
        {
            get
            {
                return remoteServices;
            }
        }

        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, mode);
        }
    }

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    public class DistributedThread<TFunc>
    {
        private IExecutor executor;

        private TFunc function;

        private DistributedThread()
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
                    var service = DistributedThread.RemoteServices.FirstOrDefault();
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
