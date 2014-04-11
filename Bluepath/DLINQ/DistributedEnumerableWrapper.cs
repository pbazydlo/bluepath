using Bluepath.DLINQ.Enumerables;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ
{
    internal class DistributedEnumerableWrapper<T> : DistributedQuery<T> where T : new()
    {
        private readonly IEnumerable<T> wrappedEnumerable;

        internal DistributedEnumerableWrapper(IEnumerable<T> wrappedEnumerable, IExtendedStorage storage)
            : base(new DistributedQuerySettings())
        {
            var key = string.Format("_queryData_{0}", Guid.NewGuid());
            this.Settings.CollectionKey = key;
            this.Settings.Storage = storage;
            var distributedList = new DistributedList<T>(storage, this.Settings.CollectionKey);
            distributedList.AddRange(wrappedEnumerable);
            this.wrappedEnumerable = distributedList;
        }

        internal IEnumerable<T> WrappedEnumerable
        {
            get { return this.wrappedEnumerable; }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return this.wrappedEnumerable.GetEnumerator();
        }
    }
}
