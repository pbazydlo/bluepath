using Bluepath.DLINQ.Enumerables;
using Bluepath.Framework;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
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
            var func = new Func<GroupByQueryArguments<TSource, TGroupKey, TElement>, IBluepathCommunicationFramework, IGrouping<TGroupKey, TElement>>(
                (args, framework) =>
                {
                    if (!(framework.Storage is IExtendedStorage))
                    {
                        throw new ArgumentException("Provided storage must implement IExtendedStorage interface!");
                    }

                    var storage = framework.Storage as IExtendedStorage;
                    var initialCollection = new DistributedList<TSource>(storage, args.CollectionKey);
                    var sharedResult = new DistributedDictionary<TGroupKey, TElement>(storage, args.ResultCollectionKey);
                    //TOutput[] result = new TOutput[args.StopIndex - args.StartIndex];
                    int index = 0;
                    for (int i = args.StartIndex; i < args.StopIndex; i++)
                    {
                        //sharedResult.Add()
                        //result[index] = args.QueryOperator(initialCollection[i]);
                        index++;
                    }

                    //DistributedList<TOutput> sharedResult = new DistributedList<TOutput>(storage, args.ResultCollectionKey);
                    //sharedResult.AddRange(result);

                    //return new UnaryQueryResult()
                    //{
                    //    CollectionKey = args.ResultCollectionKey,
                    //    CollectionType = UnaryQueryResultCollectionType.DistributedList
                    //}.Serialize();

                    return null;
                });
                
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
