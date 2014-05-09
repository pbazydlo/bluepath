using Bluepath.DLINQ.Enumerables;
using Bluepath.Framework;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
using Bluepath.Threading.Schedulers;
using Bluepath.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ.QueryOperators.Unary
{
    internal class SelectQueryOperator<TInput, TOutput> : UnaryQueryOperator<TOutput>
        //where TInput : new()
        //where TOutput : new()
    {
        private Func<TInput, TOutput> selector;

        internal SelectQueryOperator(DistributedQuery<TInput> query, Func<TInput, TOutput> selector)
            : base(query.Settings)
        {
            this.selector = selector;
        }

        protected override DistributedThread[] Execute()
        {
            //                    args,                                                             result key
            var func = new Func<UnaryQueryArguments<TInput, TOutput>, IBluepathCommunicationFramework, byte[]>(
                (args, framework) =>
                {
                    if (!(framework.Storage is IExtendedStorage))
                    {
                        throw new ArgumentException("Provided storage must implement IExtendedStorage interface!");
                    }

                    var storage = framework.Storage as IExtendedStorage;
                    var initialCollection = new DistributedList<TInput>(storage, args.CollectionKey);
                    TOutput[] result = new TOutput[args.StopIndex - args.StartIndex];
                    int index = 0;
                    for (int i = args.StartIndex; i < args.StopIndex; i++)
                    {
                        result[index] = args.QueryOperator(initialCollection[i]);
                        index++;
                    }

                    DistributedList<TOutput> sharedResult = new DistributedList<TOutput>(storage, args.ResultCollectionKey);
                    sharedResult.AddRange(result);

                    return new UnaryQueryResult()
                    {
                        CollectionKey = args.ResultCollectionKey,
                        CollectionType = UnaryQueryResultCollectionType.DistributedList
                    }.Serialize();
                });

            var collectionToProcess = new DistributedList<TInput>(this.Settings.Storage, this.Settings.CollectionKey);
            var collectionCount = collectionToProcess.Count;

            // TODO: Partition size should be calculated!
            var partitionSize = DistributedEnumerable.PartitionSize;
            var partitionNum = collectionCount / partitionSize;
            if(partitionNum==0)
            {
                partitionNum = 1;
            }

            var minItemsPerPartition = collectionCount / partitionNum;

            DistributedThread[] threads
                = new DistributedThread[partitionNum];
            var resultCollectionKey = string.Format("_selectQueryResult_{0}", Guid.NewGuid());
            for (int partNum = 0; partNum < partitionNum; partNum++)
            {
                var isLastPartition = (partNum == (partitionNum - 1));
                var args = new UnaryQueryArguments<TInput, TOutput>()
                {
                    QueryOperator = this.selector,
                    CollectionKey = this.Settings.CollectionKey,
                    ResultCollectionKey = resultCollectionKey,
                    StartIndex = (partNum * partitionSize),
                    StopIndex = isLastPartition ? collectionCount : ((partNum * partitionSize) + partitionSize)
                };

                threads[partNum] = this.CreateThread(func);
                threads[partNum].Start(args);
            }

            return threads;
        }
    }
}
