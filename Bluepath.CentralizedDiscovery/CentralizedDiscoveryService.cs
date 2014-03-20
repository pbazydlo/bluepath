using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Bluepath.CentralizedDiscovery
{
    public class CentralizedDiscoveryService : ICentralizedDiscoveryService
    {
        private static readonly List<Services.ServiceUri> AvailableServices = new List<Services.ServiceUri>();
        private static readonly object AvailableServicesLock = new object();
        public Services.ServiceUri[] GetAvailableServices()
        {
            // TODO: Possible bottleneck if many processes simultaneously try to get services.
            lock (AvailableServicesLock)
            {
                return AvailableServices.ToArray();
            }
        }

        public void Register(Services.ServiceUri uri)
        {
            lock (AvailableServicesLock)
            {
                if (AvailableServices.FirstOrDefault(s => s.Equals(uri)) != null)
                {
                    throw new ArgumentException("Service with such uri already exists!", "uri");
                }

                AvailableServices.Add(uri);
            }
        }

        public void Unregister(Services.ServiceUri uri)
        {
            lock (AvailableServicesLock)
            {
                if (AvailableServices.FirstOrDefault(s => s.Equals(uri)) == null)
                {
                    throw new ArgumentException("Service with such uri doesn't exist!", "uri");
                }

                AvailableServices.Remove(uri);
            }
        }
    }
}
