namespace Bluepath.Services.Discovery
{
    using System.Collections.Generic;

    public interface IServiceDiscovery
    {
        ICollection<ServiceReferences.IRemoteExecutorService> GetAvailableServices();
    }
}
