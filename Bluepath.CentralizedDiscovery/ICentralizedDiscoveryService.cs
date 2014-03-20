using Bluepath.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Bluepath.CentralizedDiscovery
{
    [ServiceContract]
    public interface ICentralizedDiscoveryService
    {
        [OperationContract]
        ServiceUri[] GetAvailableServices();

        [OperationContract]
        void Register(ServiceUri uri);

        [OperationContract]
        void Unregister(ServiceUri uri);
    }
}
