namespace Bluepath.CentralizedDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;

    using Bluepath.Services;

    [ServiceContract]
    public interface ICentralizedDiscoveryService
    {
        [OperationContract]
        ServiceUri[] GetAvailableServices();

        [OperationContract]
        void Register(ServiceUri uri);

        [OperationContract]
        void Unregister(ServiceUri uri);

        [OperationContract]
        Task<Dictionary<ServiceUri, PerformanceStatistics>> GetPerformanceStatistics();
    }
}
