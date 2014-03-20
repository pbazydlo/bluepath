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
        public Services.ServiceUri[] GetAvailableServices()
        {
            throw new NotImplementedException();
        }

        public void Register(Services.ServiceUri uri)
        {
            throw new NotImplementedException();
        }

        public void Unregister(Services.ServiceUri uri)
        {
            throw new NotImplementedException();
        }
    }
}
