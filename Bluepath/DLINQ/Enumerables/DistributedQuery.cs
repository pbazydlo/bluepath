using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ.Enumerables
{
    public class DistributedQuery : IEnumerable
    {
        private DistributedQuerySettings settings;

        internal DistributedQuery(DistributedQuerySettings settings)
        {
            this.settings = settings;
        }

        internal virtual DistributedQuery<TCastTo> Cast<TCastTo>()
        {
            throw new NotSupportedException();
        }

        internal virtual DistributedQuery<TCastTo> OfType<TCastTo>()
        {
            throw new NotSupportedException();
        }

        internal virtual IEnumerator GetEnumeratorUntyped()
        {
            throw new NotSupportedException();
        }

        public DistributedQuerySettings Settings
        {
            get { return this.settings; }
        }

        public IEnumerator GetEnumerator()
        {
            return GetEnumeratorUntyped();
        }
    }

    public class DistributedQuery<TSource> : DistributedQuery, IEnumerable<TSource>
        where TSource : new()
    {
        internal DistributedQuery(DistributedQuerySettings settings)
            : base(settings)
        {

        }

        internal sealed override DistributedQuery<TCastTo> Cast<TCastTo>()
        {
            throw new NotImplementedException();
            // DistributedEnumerable.Select<TSource, TCastTo>(this, elem=> (TCastTo)(object)elem)
            // TODO: ParallelEnumerable.Select<TSource, TCastTo>(this, elem => (TCastTo)(object)elem);
            // return base.Cast<TCastTo>();
        }

        internal sealed override DistributedQuery<TCastTo> OfType<TCastTo>()
        {
            throw new NotImplementedException();
            // TODO: this
                //.Where<TSource>(elem => elem is TCastTo)
                //.Select<TSource, TCastTo>(elem => (TCastTo)(object)elem);
            //return base.OfType<TCastTo>();
        }

        internal override IEnumerator GetEnumeratorUntyped()
        {
            return ((IEnumerable<TSource>)this).GetEnumerator();
        }

        public virtual IEnumerator<TSource> GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}
