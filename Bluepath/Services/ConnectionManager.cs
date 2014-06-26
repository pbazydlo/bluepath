namespace Bluepath.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Services.Discovery;

    public class ConnectionManager : IConnectionManager, IDisposable
    {
        private static readonly object DefaultLock = new object();

        private static ConnectionManager defaultConnectionManager;

        private readonly IDictionary<ServiceUri, PerformanceStatistics> remoteServices;

        private readonly object remoteServicesLock = new object();

        private readonly IServiceDiscovery serviceDiscovery;

        private readonly TimeSpan serviceDiscoveryPeriod;

        private readonly Thread serviceDiscoveryThread;

        private bool shouldStop = false;

        public ConnectionManager(
            KeyValuePair<ServiceUri,PerformanceStatistics>? remoteService,
            IListener listener,
            IServiceDiscovery serviceDiscovery = null,
            TimeSpan? serviceDiscoveryPeriod = null)
            : this(
                remoteService.HasValue ? new Dictionary<ServiceUri, PerformanceStatistics>() { {remoteService.Value.Key, remoteService.Value.Value} } : null,
                listener,
                serviceDiscovery,
                serviceDiscoveryPeriod)
        {
        }

        public ConnectionManager(
            IDictionary<ServiceUri, PerformanceStatistics> remoteServices,
            IListener listener,
            IServiceDiscovery serviceDiscovery = null,
            TimeSpan? serviceDiscoveryPeriod = null)
            : this(listener, serviceDiscovery, serviceDiscoveryPeriod)
        {
            if (remoteServices != null)
            {
                lock (this.remoteServicesLock)
                {
                    foreach (var remoteService in remoteServices.Keys)
                    {
                        if(!this.remoteServices.ContainsKey(remoteService))
                        {
                            this.remoteServices.Add(remoteService, remoteServices[remoteService]);
                        }
                    }
                }
            }
        }

        private ConnectionManager(IListener listener, IServiceDiscovery serviceDiscovery, TimeSpan? serviceDiscoveryPeriod)
        {
            this.remoteServices = new Dictionary<ServiceUri, PerformanceStatistics>();
            this.Listener = listener;
            if (serviceDiscovery != null)
            {
                this.serviceDiscovery = serviceDiscovery;
                this.serviceDiscoveryPeriod = serviceDiscoveryPeriod ?? new TimeSpan(hours: 0, minutes: 0, seconds: 5);
                this.serviceDiscoveryThread = new Thread(() =>
                {
                    while (!this.shouldStop)
                    {
                        var availableServices = this.serviceDiscovery.GetPerformanceStatistics();
                        lock (this.remoteServicesLock)
                        {
                            this.remoteServices.Clear();
                            foreach (var service in availableServices.Keys)
                            {
                                if (this.Listener==null || !service.Equals(this.Listener.CallbackUri))
                                {
                                    this.remoteServices.Add(service, availableServices[service]);
                                }
                            }
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

        public IDictionary<ServiceUri, PerformanceStatistics> RemoteServices
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

        public void Dispose()
        {
            this.serviceDiscoveryThread.Abort();
            this.shouldStop = true;
            this.serviceDiscoveryThread.Join();
            
        }
    }
}
