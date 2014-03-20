namespace Bluepath.Threading
{
    using System.Linq;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Services;

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    /// <typeparam name="TFunc">
    /// Delegate type of method to be run.
    /// </typeparam>
    public class DistributedThread<TFunc> : DistributedThread
    {
        private TFunc function;

        protected DistributedThread(IConnectionManager connectionManager)
            : base(connectionManager)
        {
        }

        /// <summary>
        /// Creates distributed thread using default connection manager.
        /// </summary>
        /// <param name="function">Method to be run.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        /// <exception cref="CannotInitializeDefaultConnectionManagerException">Indicates that default connection manager couldn't be retrieved.</exception>
        public static DistributedThread<TFunc> Create(TFunc function, DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOnly)
        {
            return new DistributedThread<TFunc>(Services.ConnectionManager.Default)
            {
                function = function,
                Mode = mode
            };
        }

        /// <summary>
        /// Creates distributed thread using supplied connection manager.
        /// </summary>
        /// <param name="function">Method to be run.</param>
        /// <param name="connectionManager">Connection manager.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        public static DistributedThread<TFunc> Create(TFunc function, IConnectionManager connectionManager, DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOnly)
        {
            return new DistributedThread<TFunc>(connectionManager)
            {
                function = function,
                Mode = mode
            };
        }

        /// <summary>
        /// Starts execution of distributed thread.
        /// </summary>
        /// <param name="parameters">Parameters for the method.</param>
        /// <exception cref="MissingRemoteServiceReferenceException">
        /// Thrown if thread is required to run on remote executor but connection manager is missing references to remote executors.
        /// </exception>
        public override void Start(params object[] parameters)
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
                        throw new MissingRemoteServiceReferenceException("No remote service was specified in DistributedThread.RemoteServices.");
                    }

                    var callbackUri = this.ConnectionManager.Listener != null ? this.ConnectionManager.Listener.CallbackUri.Convert() : null;

                    remoteExecutor.Setup(service, callbackUri);
                    remoteExecutor.Initialize(this.function);
                    RemoteExecutorService.RegisterRemoteExecutor(remoteExecutor);
                    this.Executor = remoteExecutor;
                    break;
            }

            this.Executor.Execute(parameters);
        }
    }
}
