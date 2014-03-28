namespace Bluepath.Services.Discovery
{
    using System.Collections.Generic;

    public interface IServiceDiscovery
    {
        ICollection<ServiceUri> GetAvailableServices();

        Dictionary<ServiceUri, PerformanceStatistics> GetPerformanceStatistics();
    }
}
