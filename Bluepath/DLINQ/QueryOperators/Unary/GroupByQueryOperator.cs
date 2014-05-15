using Bluepath.DLINQ.Enumerables;
using Bluepath.Framework;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
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
            // IGrouping<TGroupKey, TElement>
            var func = new Func<GroupByQueryArguments<TSource, TGroupKey, TElement>, IBluepathCommunicationFramework, UnaryQueryResult>(
                (args, framework) =>
                {
                    if (!(framework.Storage is IExtendedStorage))
                    {
                        throw new ArgumentException("Provided storage must implement IExtendedStorage interface!");
                    }

                    var storage = framework.Storage as IExtendedStorage;
                    var initialCollection = new DistributedList<TSource>(storage, args.CollectionKey);
                    var sharedResult = new DistributedDictionary<TGroupKey, DistributedList<TElement>>(
                        storage,
                        args.ResultCollectionKey,
                        args.Comparer
                        );
                    Dictionary<TGroupKey, DistributedList<TElement>> localResult
                        = new Dictionary<TGroupKey, DistributedList<TElement>>(args.Comparer);
                    int index = 0;
                    for (int i = args.StartIndex; i < args.StopIndex; i++)
                    {
                        var key = args.KeySelector(initialCollection[i]);
                        var element = args.ElementSelector(initialCollection[i]);
                        DistributedList<TElement> keyList;
                        if (!localResult.ContainsKey(key))
                        {
                            try
                            {
                                // create distributed list, and try add it to distributed dictionary
                                // if the list has already been added ArgumentException will be thrown
                                keyList = new DistributedList<TElement>(storage, string.Format("_groupByQueryList{0}_{1}", args.ResultCollectionKey, key));
                                sharedResult.Add(key, keyList);
                                localResult[key] = keyList;
                            }
                            catch (ArgumentException)
                            {
                                // specified key already exists - just fetch the list
                                keyList = sharedResult[key];
                            }
                        }
                        else
                        {
                            keyList = localResult[key];
                        }

                        if (keyList.Storage == null)
                        {
                            keyList.Storage = storage;
                        }

                        keyList.Add(element);
                        index++;
                    }

                    //DistributedList<TOutput> sharedResult = new DistributedList<TOutput>(storage, args.ResultCollectionKey);
                    //sharedResult.AddRange(result);

                    //return new UnaryQueryResult()
                    //{
                    //    CollectionKey = args.ResultCollectionKey,
                    //    CollectionType = UnaryQueryResultCollectionType.DistributedList
                    //}.Serialize();

                    return new UnaryQueryResult()
                        {
                            CollectionKey = args.ResultCollectionKey,
                            CollectionType = UnaryQueryResultCollectionType.DistributedDictionary
                        };
                });

            var collectionToProcess = new DistributedList<TSource>(this.Settings.Storage, this.Settings.CollectionKey);
            var collectionCount = collectionToProcess.Count;

            // TODO: Partition size should be calculated!
            var partitionSize = DistributedEnumerable.PartitionSize;
            var partitionNum = collectionCount / partitionSize;
            if (partitionNum == 0)
            {
                partitionNum = 1;
            }

            var minItemsPerPartition = collectionCount / partitionNum;

            DistributedThread[] threads
                = new DistributedThread[partitionNum];
            var resultCollectionKey = string.Format("_groupByQueryResult_{0}", Guid.NewGuid());
            for (int partNum = 0; partNum < partitionNum; partNum++)
            {
                var isLastPartition = (partNum == (partitionNum - 1));
                var args = new GroupByQueryArguments<TSource, TGroupKey, TElement>()
                {
                    CollectionKey = this.Settings.CollectionKey,
                    Comparer = this.comparer,
                    ElementSelector = this.elementSelector,
                    KeySelector = this.keySelector,
                    StartIndex = (partNum * partitionSize),
                    StopIndex = isLastPartition ? collectionCount : ((partNum * partitionSize) + partitionSize),
                    ResultCollectionKey = resultCollectionKey
                };

                threads[partNum] = this.CreateThread(func);
                threads[partNum].Start(args);
            }

            return threads;
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
