namespace Bluepath.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Services.Discovery;

    public class ConnectionManager : IConnectionManager
    {
        private static readonly object DefaultLock = new object();

        private static ConnectionManager defaultConnectionManager;

        private readonly List<ServiceReferences.IRemoteExecutorService> remoteServices;

        private IServiceDiscovery serviceDiscovery;

        private TimeSpan serviceDiscoveryPeriod;

        private Thread serviceDiscoveryThread;

        public ConnectionManager(ServiceReferences.IRemoteExecutorService remoteService, IListener listener,
            IServiceDiscovery serviceDiscovery = null, TimeSpan? serviceDiscoveryPeriod = null)
            : this(remoteService != null ?
                    new List<ServiceReferences.IRemoteExecutorService>() { remoteService }
                    : null,
            listener, serviceDiscovery, serviceDiscoveryPeriod)
        {
        }

        public ConnectionManager(IEnumerable<ServiceReferences.IRemoteExecutorService> remoteServices, IListener listener,
            IServiceDiscovery serviceDiscovery = null, TimeSpan? serviceDiscoveryPeriod = null)
            : this(listener, serviceDiscovery, serviceDiscoveryPeriod)
        {
            if (remoteServices != null)
            {
                this.remoteServices.AddRange(remoteServices);
            }
        }

        private ConnectionManager(IListener listener, IServiceDiscovery serviceDiscovery, TimeSpan? serviceDiscoveryPeriod)
        {
            this.remoteServices = new List<ServiceReferences.IRemoteExecutorService>();
            this.Listener = listener;
            this.serviceDiscoveryThread = new Thread(() =>
            {
                while(true)
                {
                    // var availableServices = this.serviceDiscovery.AvailableServices;
                    // new TimeSpan(hours: 0, minutes: 0, seconds: 5)
                }
            });
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

                        ConnectionManager.defaultConnectionManager = new ConnectionManager(BluepathListener.Default, null, null);
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
