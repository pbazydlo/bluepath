﻿namespace Bluepath.Threading
{
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Services;
using Bluepath.Threading.Schedulers;

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    /// <typeparam name="TFunc">
    /// Delegate type of method to be run.
    /// </typeparam>
    public class DistributedThread<TFunc> : DistributedThread
    {
        private TFunc function;

        protected DistributedThread(IConnectionManager connectionManager, IScheduler scheduler)
            : base(connectionManager, scheduler)
        {
        }

        /// <summary>
        /// Creates distributed thread using default connection manager.
        /// </summary>
        /// <param name="function">Method to be run.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        /// <exception cref="CannotInitializeDefaultConnectionManagerException">Indicates that default connection manager couldn't be retrieved.</exception>
        public static DistributedThread<TFunc> Create(
            TFunc function,
            DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOnly
            )
        {
            return new DistributedThread<TFunc>(
                Services.ConnectionManager.Default, 
                new ThreadNumberScheduler(Services.ConnectionManager.Default)
                )
            {
                function = function,
                Mode = mode
            };
        }

        /// <summary>
        /// Creates distributed thread using default connection manager.
        /// </summary>
        /// <param name="function">Method to be run.</param>
        /// <param name="scheduler">Scheduler wich will be used to select executing site.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        /// <exception cref="CannotInitializeDefaultConnectionManagerException">Indicates that default connection manager couldn't be retrieved.</exception>
        public static DistributedThread<TFunc> Create(
            TFunc function, 
            IScheduler scheduler,
            DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOnly
            )
        {
            return new DistributedThread<TFunc>(Services.ConnectionManager.Default, scheduler)
            {
                function = function,
                Mode = mode
            };
        }

        /// <summary>
        /// Creates distributed thread using supplied connection manager.
        /// </summary>
        /// <param name="function">Method to be run.</param>
        /// <param name="scheduler">Scheduler wich will be used to select executing site.</param>
        /// <param name="connectionManager">Connection manager.</param>
        /// <param name="mode">Executor selection strategy.</param>
        /// <returns>Instance of distributed thread.</returns>
        public static DistributedThread<TFunc> Create(
            TFunc function,
            IConnectionManager connectionManager, 
            IScheduler scheduler,
            DistributedThread.ExecutorSelectionMode mode = DistributedThread.ExecutorSelectionMode.RemoteOrLocal
            )
        {
            return new DistributedThread<TFunc>(connectionManager, scheduler)
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

                    parameters = this.DeepCopy(parameters);

                    break;
                case DistributedThread.ExecutorSelectionMode.RemoteOrLocal:
                case DistributedThread.ExecutorSelectionMode.RemoteOnly:
                    var remoteExecutor = new RemoteExecutor();
                    var service = this.Scheduler.GetRemoteService();
                    if (service == null)
                    {
                        if (this.Mode == DistributedThread.ExecutorSelectionMode.RemoteOnly)
                        {
                            throw new MissingRemoteServiceReferenceException("No remote service was specified in DistributedThread.RemoteServices.");
                        }
                        else
                        {
                            // No remote services available - switching to local only mode
                            this.Mode = DistributedThread.ExecutorSelectionMode.LocalOnly;
                            this.Start(parameters);
                            return;
                        }
                    }

                    ServiceReferences.ServiceUri callbackUri = null;
                    if(this.ConnectionManager!=null && this.ConnectionManager.Listener != null)
                    {
                        callbackUri = this.ConnectionManager.Listener.CallbackUri.Convert();
                    }

                    remoteExecutor.Setup(service, callbackUri);
                    remoteExecutor.Initialize(this.function);
                    RemoteExecutorService.RegisterRemoteExecutor(remoteExecutor);
                    this.Executor = remoteExecutor;
                    break;
            }

            this.Executor.Execute(parameters);
        }

        public object[] DeepCopy(object objectToCopy)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, objectToCopy);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (object[])binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
