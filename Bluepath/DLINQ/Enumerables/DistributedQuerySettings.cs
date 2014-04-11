using Bluepath.Storage;
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
    }
}
