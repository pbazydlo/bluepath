namespace Bluepath.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Bluepath.Executor;
    using Bluepath.Extensions;

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    public class DistributedThread
    {
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static readonly List<ServiceReferences.IRemoteExecutorService> remoteServices = new List<ServiceReferences.IRemoteExecutorService>();

        private IExecutor executor;

        private Func<object[], object> function;

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
                return DistributedThread.remoteServices;
            }
        }

        public ExecutorSelectionMode Mode { get; private set; }

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

        public static DistributedThread Create(Func<object[], object> function, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return new DistributedThread()
            {
                function = function,
                Mode = mode
            };
        }

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
                    var service = DistributedThread.RemoteServices.FirstOrDefault();
                    if (service == null)
                    {
                        throw new NullReferenceException("No remote service was specified in DistributedThread.RemoteServices!");
                    }

                    remoteExecutor.Initialize(service, this.function, BluepathSingleton.Instance.CallbackUri.Convert());
                    System.Threading.Thread.Sleep(2000);
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
