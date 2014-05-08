using Bluepath.DLINQ.Enumerables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ.QueryOperators.Unary
{
    public class GroupByQueryOperator<TSource, TGroupKey, TElement>
        : UnaryQueryOperator<IGrouping<TGroupKey, TElement>>
    {
        private Func<TSource, TGroupKey> keySelector;
        private Func<TSource, TElement> elementSelector;
        private IEqualityComparer<TGroupKey> comparer;

        internal GroupByQueryOperator(DistributedQuery<TSource> source,
            Func<TSource, TGroupKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TGroupKey> comparer)
            : base(source.Settings)
        {
            this.keySelector = keySelector;
            this.elementSelector = elementSelector;
            if (elementSelector == null)
            {
                this.elementSelector = (src) => (TElement)(object)src;
            }

            this.comparer = comparer;
            if (comparer == null)
            {
                this.comparer = EqualityComparer<TGroupKey>.Default;
            }
        }

        protected override Threading.DistributedThread[] Execute()
        {
            return base.Execute();
        }

        [Serializable]
        private class GroupByQueryArguments<TSo, TGrK, TEl> 
            : UnaryQueryArguments<TSo, IGrouping<TGrK, TEl>>
        {
            public Func<TSource, TGroupKey> KeySelector { get; set; }

            public Func<TSource, TElement> ElementSelector { get; set; }

            public IEqualityComparer<TGroupKey> Comparer { get; set; }
        }
    }
}
