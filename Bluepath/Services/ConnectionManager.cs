namespace Bluepath.Services
{
    using System.Collections.Generic;

    using Bluepath.Exceptions;

    public class ConnectionManager : IConnectionManager
    {
        private static readonly object DefaultLock = new object();

        private static ConnectionManager defaultConnectionManager;

        private readonly List<ServiceReferences.IRemoteExecutorService> remoteServices;

        public ConnectionManager(ServiceReferences.IRemoteExecutorService remoteService, IListener listener)
            : this(remoteService != null ?
                    new List<ServiceReferences.IRemoteExecutorService>() { remoteService }
                    : null,
            listener)
        {
        }

        public ConnectionManager(IEnumerable<ServiceReferences.IRemoteExecutorService> remoteServices, IListener listener)
            : this(listener)
        {
            if (remoteServices != null)
            {
                this.remoteServices.AddRange(remoteServices);
            }
        }

        private ConnectionManager(IListener listener)
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
