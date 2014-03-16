namespace Bluepath.Threading
{
    using System.Collections.Generic;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.Services;

    public abstract class DistributedThread
    {
        protected readonly IConnectionManager ConnectionManager;

        protected DistributedThread(IConnectionManager connectionManager)
        {
            this.ConnectionManager = connectionManager;
        }

        /// <summary>
        /// Executor selection mode
        /// </summary>
        public enum ExecutorSelectionMode : int
        {
            /// <summary>
            /// Run on local executor.
            /// </summary>
            LocalOnly = 0,

            /// <summary>
            /// Run on remote executor.
            /// </summary>
            RemoteOnly = 1
        }

        /// <summary>
        /// Gets list of known remote executor services.
        /// </summary>
        public IEnumerable<ServiceReferences.IRemoteExecutorService> RemoteServices
        {
            get
            {
                return this.ConnectionManager.RemoteServices;
            }
        }

        /// <summary>
        /// Gets executor selection mode.
        /// </summary>
        public ExecutorSelectionMode Mode { get; protected set; }

        /// <summary>
        /// Gets underlying executor's state.
        /// </summary>
        public ExecutorState State
        {
            get
            {
                return this.Executor.ExecutorState;
            }
        }

        /// <summary>
        /// Gets processing result from executor. 
        /// Make sure to call Join first. If the result is not available exception will be thrown.
        /// </summary>
        /// <exception cref="ResultNotAvailableException">Thrown if executor is still running.</exception>
        public object Result
        {
            get
            {
                return this.Executor.Result;
            }
        }

        protected IExecutor Executor { get; set; }

        /// <summary>
        /// Creates distributed thread using default connection manager.
        /// </summary>
        /// <typeparam name="TFunc">Delegate type of method to be run.</typeparam>
        /// <param name="function">Method to be run.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        /// <exception cref="CannotInitializeDefaultConnectionManagerException">Indicates that default connection manager couldn't be retrieved.</exception>
        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, mode);
        }

        /// <summary>
        /// Creates distributed thread using supplied connection manager.
        /// </summary>
        /// <typeparam name="TFunc">Delegate type of method to be run.</typeparam>
        /// <param name="function">Method to be run.</param>
        /// <param name="connectionManager">Connection manager.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        public static DistributedThread<TFunc> Create<TFunc>(TFunc function, IConnectionManager connectionManager, ExecutorSelectionMode mode = ExecutorSelectionMode.RemoteOnly)
        {
            return DistributedThread<TFunc>.Create(function, connectionManager, mode);
        }

        /// <summary>
        /// Starts execution of distributed thread.
        /// </summary>
        /// <param name="parameters">Parameters for the method.</param>
        public abstract void Start(params object[] parameters);

        /// <summary>
        /// Blocks calling thread while underlying local or remote thread is running.
        /// </summary>
        /// <exception cref="RemoteException">Rethrows exception that occurred on the remote executor.</exception>
        /// <exception cref="RemoteJoinAbortedException">Thrown if join thread ends unexpectedly (eg. endpoint was not found).</exception>
        /// <exception cref="ThreadInterruptedException">Thrown if the thread is interrupted while waiting.</exception>
        /// <exception cref="ThreadStateException">Thrown if the thread has not been started yet.</exception>
        public void Join()
        {
            this.Executor.Join();
        }
    }
}
