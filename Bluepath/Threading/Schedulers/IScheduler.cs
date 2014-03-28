using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Threading.Schedulers
{
    public interface IScheduler
    {
        ServiceReferences.IRemoteExecutorService GetRemoteService();
    }
}
