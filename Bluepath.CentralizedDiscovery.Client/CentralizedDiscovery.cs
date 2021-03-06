﻿namespace Bluepath.CentralizedDiscovery.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Bluepath.CentralizedDiscovery.Client.Extensions;
    using Bluepath.Services;

    public class CentralizedDiscovery : Bluepath.Services.Discovery.IServiceDiscovery, IDisposable
    {
        private readonly List<ServiceUri> services;
        private readonly object servicesLock = new object();
        private readonly ServiceReferences.CentralizedDiscoveryServiceClient client;
        private readonly Bluepath.Services.ServiceUri listenerUri;

        public CentralizedDiscovery(Bluepath.Services.ServiceUri masterUri, Bluepath.Services.BluepathListener listener)
        {
            this.services = new List<ServiceUri>();
            this.client = new ServiceReferences.CentralizedDiscoveryServiceClient(masterUri.Binding, masterUri.ToEndpointAddress());
            this.listenerUri = listener.CallbackUri;
            var registerThread = new Thread(() =>
                    this.client.Register(this.ConvertToClientServiceUri(this.listenerUri)));
            registerThread.Start();
        }

        public ICollection<ServiceUri> GetAvailableServices()
        {
            lock (this.servicesLock)
            {
                var availableServices = this.client.GetAvailableServices()
                    .Select(this.ConvertToBluepathServiceUri)
                    .Where(s => !s.Equals(this.listenerUri)).ToArray();
                var newServices = availableServices.Where(s => !this.services.Contains(s)).ToArray();
                var servicesToDelete = this.services.Where(k => !availableServices.Contains(k)).ToArray();
                foreach (var service in servicesToDelete)
                {
                    this.services.Remove(service);
                }

                this.services.AddRange(newServices);

                return this.services.ToArray();
            }
        }

        public Dictionary<ServiceUri, PerformanceStatistics> GetPerformanceStatistics()
        {
            var performanceStatistics = new Dictionary<ServiceUri, PerformanceStatistics>();
            foreach (var ps in this.client.GetPerformanceStatistics())
            {
                // TODO: Extension method to case ServiceUri
                performanceStatistics.Add(new ServiceUri() { Address = ps.Key.Address, BindingType = (BindingType)ps.Key.BindingType}, ps.Value.FromServiceReference());
            }

            return performanceStatistics;
        }

        public void Dispose()
        {
            this.client.Unregister(this.ConvertToClientServiceUri(this.listenerUri));
            this.client.Close();
            if (this.client is IDisposable)
            {
                (this.client as IDisposable).Dispose();
            }
        }

        private ServiceReferences.ServiceUri ConvertToClientServiceUri(Bluepath.Services.ServiceUri uri)
        {
            if (uri == null)
            {
                return null;
            }

            return new ServiceReferences.ServiceUri()
            {
                Address = uri.Address,
                BindingType = (ServiceReferences.BindingType)((int)uri.BindingType)
            };
        }

        private Bluepath.Services.ServiceUri ConvertToBluepathServiceUri(ServiceReferences.ServiceUri uri)
        {
            if (uri == null)
            {
                return null;
            }

            return new Bluepath.Services.ServiceUri()
            {
                Address = uri.Address,
                BindingType = (Bluepath.Services.BindingType)((int)uri.BindingType)
            };
        }
    }
}
