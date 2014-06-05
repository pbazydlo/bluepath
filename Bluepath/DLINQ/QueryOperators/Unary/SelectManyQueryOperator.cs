using Bluepath.DLINQ.Enumerables;
using Bluepath.Framework;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
using Bluepath.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ.QueryOperators.Unary
{
    public class SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> : UnaryQueryOperator<TOutput>
    {
        private Func<TLeftInput, IEnumerable<TRightInput>> rightChildSelector;
        private Func<TLeftInput, int, IEnumerable<TRightInput>> indexedRightChildSelector;
        private Func<TLeftInput, TRightInput, TOutput> resultSelector;

        internal SelectManyQueryOperator(DistributedQuery<TLeftInput> leftChild,
                                         Func<TLeftInput, IEnumerable<TRightInput>> rightChildSelector,
                                         Func<TLeftInput, int, IEnumerable<TRightInput>> indexedRightChildSelector,
                                         Func<TLeftInput, TRightInput, TOutput> resultSelector)
            : base(leftChild)
        {
            this.rightChildSelector = rightChildSelector;
            this.indexedRightChildSelector = indexedRightChildSelector;
            if(resultSelector!=null)
            {
                this.resultSelector = resultSelector;
            }
            else
            {
                this.resultSelector = (leftIn, rightIn) =>
                    {
                        if(rightIn is TOutput)
                        {
                            return (TOutput)(object)rightIn;
                        }

                        return default(TOutput);
                    };
            }

        }

        protected override DistributedThread[] Execute()
        {
            //                    args,                                                             result key
            var func = new Func<SelectManyQueryArguments<TLeftInput, TRightInput, TOutput>, IBluepathCommunicationFramework, byte[]>(
                (args, framework) =>
                {
                    if (!(framework.Storage is IExtendedStorage))
                    {
                        throw new ArgumentException("Provided storage must implement IExtendedStorage interface!");
                    }

                    var storage = framework.Storage as IExtendedStorage;
                    var initialCollection = new DistributedList<TLeftInput>(storage, args.CollectionKey);
                    //List<TRightInput> rightChild = new List<TRightInput>();
                    List<TOutput> result = new List<TOutput>();
                    for (int i = args.StartIndex; i < args.StopIndex; i++)
                    {
                        var leftChild = initialCollection[i];
                        IEnumerable<TRightInput> rightChildren = null;
                        if (args.QueryOperator != null)
                        {
                            rightChildren = args.QueryOperator(leftChild);
                        }
                        else if (args.IndexedRightChildSelector != null)
                        {
                            rightChildren = args.IndexedRightChildSelector(leftChild, i);
                        }

                        foreach (var rightChild in rightChildren)
                        {
                            result.Add(args.ResultSelector(leftChild, rightChild));
                        }
                    }

                    DistributedList<TOutput> sharedResult = new DistributedList<TOutput>(storage, args.ResultCollectionKey);
                    sharedResult.AddRange(result);

                    return new UnaryQueryResult()
                    {
                        CollectionKey = args.ResultCollectionKey,
                        CollectionType = UnaryQueryResultCollectionType.DistributedList
                    }.Serialize();
                });

            var collectionToProcess = new DistributedList<TLeftInput>(this.Settings.Storage, this.Settings.CollectionKey);
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
            var resultCollectionKey = string.Format("_selectManyQueryResult_{0}", Guid.NewGuid());
            for (int partNum = 0; partNum < partitionNum; partNum++)
            {
                var isLastPartition = (partNum == (partitionNum - 1));
                var args = new SelectManyQueryArguments<TLeftInput, TRightInput, TOutput>()
                {
                    QueryOperator = this.rightChildSelector,
                    IndexedRightChildSelector = this.indexedRightChildSelector,
                    ResultSelector = this.resultSelector,
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

        [Serializable]
        private class SelectManyQueryArguments<TLI, TRI, TO> : UnaryQueryArguments<TLI, IEnumerable<TRI>>
        {
            public Func<TLI, int, IEnumerable<TRI>> IndexedRightChildSelector { get; set; }

            public Func<TLI, TRI, TO> ResultSelector { get; set; }
        }
    }
}
