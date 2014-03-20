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

        private readonly object remoteServicesLock = new object();

        private readonly IServiceDiscovery serviceDiscovery;

        private readonly TimeSpan serviceDiscoveryPeriod;

        private readonly Thread serviceDiscoveryThread;

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
                lock (this.remoteServicesLock)
                {
                    this.remoteServices.AddRange(remoteServices);
                }
            }
        }

        private ConnectionManager(IListener listener, IServiceDiscovery serviceDiscovery, TimeSpan? serviceDiscoveryPeriod)
        {
            this.remoteServices = new List<ServiceReferences.IRemoteExecutorService>();
            this.Listener = listener;
            if (serviceDiscovery != null)
            {
                this.serviceDiscovery = serviceDiscovery;
                this.serviceDiscoveryPeriod = serviceDiscoveryPeriod ?? new TimeSpan(hours: 0, minutes: 0, seconds: 5);
                this.serviceDiscoveryThread = new Thread(() =>
                {
                    while (true)
                    {
                        var availableServices = this.serviceDiscovery.GetAvailableServices();
                        lock (this.remoteServicesLock)
                        {
                            // What about services parmanently removed, where should they be disposed?
                            this.remoteServices.Clear();
                            this.remoteServices.AddRange(availableServices);
                        }

                        Thread.Sleep(this.serviceDiscoveryPeriod);
                    }
                });

                this.serviceDiscoveryThread.Start();
            }
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
                lock (this.remoteServicesLock)
                {
                    return this.remoteServices;
                }
            }
        }

        public IListener Listener { get; private set; }
    }
}
