#if BLUEPATH_CORE
namespace Bluepath.Extensions
#endif
#if BLUEPATH_CENTRALIZEDDISCOVERY_CLIENT
namespace Bluepath.CentralizedDiscovery.Client.Extensions
#endif
{
    using System.Collections.Generic;

    using Bluepath.Executor;
    using Bluepath.Services;

    public static class PerformanceStatisticsExtensions
    {
        public static PerformanceStatistics FromServiceReference(this ServiceReferences.PerformanceStatistics sr)
        {
            var performanceStatistics = new PerformanceStatistics();
            performanceStatistics.NumberOfTasks = new Dictionary<ExecutorState, int>();

            foreach (var p in sr.NumberOfTasks)
            {
                performanceStatistics.NumberOfTasks.Add((ExecutorState)p.Key, p.Value);
            }

            return performanceStatistics;
        }
    }
}
