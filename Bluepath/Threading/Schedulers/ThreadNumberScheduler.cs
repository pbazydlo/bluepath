using Bluepath.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Threading.Schedulers
{
    public class ThreadNumberScheduler : IScheduler
    {
        private IConnectionManager connectionManager;

        public ThreadNumberScheduler(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        public ServiceUri GetRemoteServiceUri()
        {
            var remoteServices = this.connectionManager.RemoteServices;
            if(remoteServices.Count==0)
            {
                return null;
            }

            var minNumberOfTasks = remoteServices.Min(rs => CountTasks(rs));
            var serviceData = remoteServices.Where(rs => CountTasks(rs) == minNumberOfTasks).First();
            if (!serviceData.Value.NumberOfTasks.ContainsKey(Executor.ExecutorState.Running))
            {
                serviceData.Value.NumberOfTasks.Add(Executor.ExecutorState.Running, 0);
            }

            serviceData.Value.NumberOfTasks[Executor.ExecutorState.Running] += 1;
            return serviceData.Key;
        }

        private static int CountTasks(KeyValuePair<ServiceUri, PerformanceStatistics> rs)
        {
            return rs.Value.NumberOfTasks
                                .Where(
                                task =>
                                    task.Key != Executor.ExecutorState.Finished &&
                                    task.Key != Executor.ExecutorState.Faulted
                                    )
                                .Sum(task => task.Value);
        }

        public ServiceReferences.IRemoteExecutorService GetRemoteService()
        {
            var serviceUri = this.GetRemoteServiceUri();
            if (serviceUri == null)
            {
                return null;
            }

            return new Bluepath.ServiceReferences.RemoteExecutorServiceClient(serviceUri.Binding, serviceUri.ToEndpointAddress());
        }
    }
}
