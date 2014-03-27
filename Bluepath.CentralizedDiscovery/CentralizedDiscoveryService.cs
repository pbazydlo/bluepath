namespace Bluepath.CentralizedDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Bluepath.Extensions;
    using Bluepath.Services;

    public class CentralizedDiscoveryService : ICentralizedDiscoveryService
    {
        private static readonly List<Services.ServiceUri> AvailableServices = new List<Services.ServiceUri>();
        private static readonly object AvailableServicesLock = new object();

        public Services.ServiceUri[] GetAvailableServices()
        {
            // Possible bottleneck if many processes simultaneously try to get services.
            // TODO: Solve this like readers-writers problem
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

        /// <summary>
        /// Gets performance statistics for each known node. 
        /// This operation can take significant amount of time.
        /// </summary>
        /// <returns>Performance statistics for each node.</returns>
        public async Task<Dictionary<ServiceUri, PerformanceStatistics>> GetPerformanceStatistics()
        {
            var performanceStatistics = new Dictionary<ServiceUri, PerformanceStatistics>();

            // snapshot services list
            var availableServices = default(List<ServiceUri>);
            lock (AvailableServicesLock)
            {
                availableServices = AvailableServices.Skip(0).ToList();
            }

            // get performance statistics from each node
            foreach (var service in availableServices)
            {
                using (var client = new Bluepath.ServiceReferences.RemoteExecutorServiceClient(
                    service.Binding,
                    service.ToEndpointAddress()))
                {
                    Log.TraceMessage(string.Format("[PerfStat] Getting performance statistics from '{0}'.", service.Address));
                    performanceStatistics.Add(service, (await client.GetPerformanceStatisticsAsync()).FromServiceReference());
                }
            }

            return performanceStatistics;
        }
    }
}
