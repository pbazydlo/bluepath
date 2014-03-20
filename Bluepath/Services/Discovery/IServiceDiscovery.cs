using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Services.Discovery
{
    public interface IServiceDiscovery
    {
        ICollection<ServiceReferences.IRemoteExecutorService> GetAvailableServices();
    }
}
