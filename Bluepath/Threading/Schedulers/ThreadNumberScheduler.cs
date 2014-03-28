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

        public ServiceReferences.IRemoteExecutorService GetRemoteService()
        {
            var remoteServices = this.connectionManager.RemoteServices;
            var minNumberOfTasks = remoteServices.Min(rs=>rs.Value.NumberOfTasks);
            var serviceData = remoteServices.Where(rs => rs.Value.NumberOfTasks == minNumberOfTasks).First();
            var serviceUri = serviceData.Key;
            return new Bluepath.ServiceReferences.RemoteExecutorServiceClient(serviceUri.Binding, serviceUri.ToEndpointAddress());
        }
    }
}
