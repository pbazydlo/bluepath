namespace Bluepath.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Bluepath.Exceptions;

    public class ConnectionManager : IConnectionManager
    {
        private static readonly object DefaultLock = new object();

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static ConnectionManager defaultConnectionManager;

        private readonly List<ServiceReferences.IRemoteExecutorService> remoteServices;

        public ConnectionManager(ServiceReferences.IRemoteExecutorService remoteService, BluepathListener listener)
            : this(new List<ServiceReferences.IRemoteExecutorService>() { remoteService }, listener)
        {
        }

        public ConnectionManager(IEnumerable<ServiceReferences.IRemoteExecutorService> remoteServices, BluepathListener listener)
            : this(listener)
        {
            this.remoteServices.AddRange(remoteServices);
        }

        private ConnectionManager(BluepathListener listener)
        {
            this.remoteServices = new List<ServiceReferences.IRemoteExecutorService>();
            this.Listener = listener;
        }

        public static ConnectionManager Default
        {
            get
            {
                lock (ConnectionManager.DefaultLock)
                {
                    if (ConnectionManager.defaultConnectionManager == null)
                    {
                        if (BluepathListener.Default == null)
                        {
                            throw new CannotInitializeDefaultConnectionManagerException("Can't create default connection manager. Initialize default listener (using BluepathListener.InitializeDefaultListener) first.");
                        }

                        ConnectionManager.defaultConnectionManager = new ConnectionManager(BluepathListener.Default);
                    }
                }

                return ConnectionManager.defaultConnectionManager;
            }
        }

        public IEnumerable<ServiceReferences.IRemoteExecutorService> RemoteServices
        {
            get
            {
                return this.remoteServices;
            }
        }

        public IListener Listener { get; private set; }
    }
}
