namespace Bluepath.Threading.Schedulers
{
    using Bluepath.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RoundRobinLocalScheduler : IScheduler
    {
        private List<ServiceUri> availableServices;
        private int nextServiceUri = 0;

        public RoundRobinLocalScheduler(ServiceUri[] availableServices)
        {
            this.availableServices = new List<ServiceUri>(availableServices);
        }

        public ServiceUri GetRemoteServiceUri()
        {
            if (this.availableServices.Count == 0)
            {
                return null;
            }

            var returnUri = this.availableServices[nextServiceUri];
            nextServiceUri = (nextServiceUri + 1) % this.availableServices.Count;
            return returnUri;
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
