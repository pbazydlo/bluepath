using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath.CentralizedDiscovery.Client
{
    public class CentralizedDiscovery : Bluepath.Services.Discovery.IServiceDiscovery, IDisposable
    {
        private readonly Dictionary<Bluepath.Services.ServiceUri, Bluepath.ServiceReferences.RemoteExecutorServiceClient> services;
        private object servicesLock = new object();
        private ServiceReferences.CentralizedDiscoveryServiceClient client;
        private Bluepath.Services.ServiceUri listenerUri;

        public CentralizedDiscovery(Bluepath.Services.ServiceUri masterUri, Bluepath.Services.BluepathListener listener)
        {
            this.services = new Dictionary<Services.ServiceUri, Bluepath.ServiceReferences.RemoteExecutorServiceClient>();
            this.client = new ServiceReferences.CentralizedDiscoveryServiceClient(masterUri.Binding, masterUri.ToEndpointAddress());
            this.listenerUri = listener.CallbackUri;
            ThreadPool.QueueUserWorkItem((threadContext) =>
                {
                    this.client.Register(this.ConvertToClientServiceUri(this.listenerUri));
                });
        }

        public ICollection<Bluepath.ServiceReferences.IRemoteExecutorService> GetAvailableServices()
        {
            lock (this.servicesLock)
            {
                var availableServices = this.client.GetAvailableServices()
                    .Select(s => this.ConvertToBluepathServiceUri(s))
                    .Where(s => !s.Equals(this.listenerUri)).ToArray();
                var newServices = availableServices.Where(s => !this.services.ContainsKey(s));
                var servicesToDelete = this.services.Keys.Where(k => !availableServices.Contains(k));
                foreach (var service in servicesToDelete)
                {
                    // TODO: What if we close connection (to non existing node) when it is in use by DistributedThread?
                    var serviceClient = this.services[service];
                    serviceClient.Close();
                    if (serviceClient is IDisposable)
                    {
                        (serviceClient as IDisposable).Dispose();
                    }

                    this.services.Remove(service);
                }

                foreach (var service in newServices)
                {
                    this.services.Add(service,
                        new Bluepath.ServiceReferences.RemoteExecutorServiceClient(
                            service.Binding,
                            service.ToEndpointAddress())
                            );
                }

                return this.services.Values.ToArray();
            }
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
