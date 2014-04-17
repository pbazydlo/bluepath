using Bluepath.Services;
using Bluepath.Storage;
using Bluepath.Threading.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ.Enumerables
{
    public class DistributedQuerySettings
    {
        public string CollectionKey { get; set; }

        public IExtendedStorage Storage { get; set; }

        public IConnectionManager DefaultConnectionManager { get; set; }

        public IScheduler DefaultScheduler { get; set; }
    }
}
