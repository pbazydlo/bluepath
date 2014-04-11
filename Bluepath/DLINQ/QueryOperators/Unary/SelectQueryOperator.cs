using Bluepath.DLINQ.Enumerables;
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
    internal class SelectQueryOperator<TInput, TOutput> : DistributedQuery<TOutput>
        where TInput : new()
        where TOutput : new()
    {
        private Func<TInput, TOutput> selector;

        internal SelectQueryOperator(DistributedQuery<TInput> query, Func<TInput, TOutput> selector)
            : base(query.Settings)
        {
            this.selector = selector;
        }

        internal void Execute()
        {
            //                    args,                               result key
            var func = new Func<SelectQueryArguments<TInput, TOutput>, IExtendedStorage, string>(
                (args, storage) =>
                {
                    var initialCollection = new DistributedList<TInput>(storage, args.CollectionKey);
                    TOutput[] result = new TOutput[initialCollection.Count];
                    int index = 0;
                    foreach (var item in initialCollection)
                    {
                        result[index] = args.Selector(item);
                        index++;
                    }

                    var resultKey = string.Format("_selectResult_{0}", Guid.NewGuid());
                    var resultCollection = new DistributedList<TOutput>(storage, resultKey);
                    resultCollection.AddRange(result);

                    return resultKey;
                });

            var collectionToProcess = new DistributedList<TInput>(this.Settings.Storage, this.Settings.CollectionKey);
            var collectionCount = collectionToProcess.Count;

            // TODO: Partition size should be calculated!
            var partitionSize = 10;
            var partitionNum = collectionCount / partitionSize;
            var minItemsPerPartition = collectionCount / partitionNum;

            DistributedThread<Func<SelectQueryArguments<TInput, TOutput>, IExtendedStorage, string>>[] threads
                = new DistributedThread<Func<SelectQueryArguments<TInput, TOutput>, IExtendedStorage, string>>[partitionNum];
            for (int partNum = 0; partNum < partitionNum; partNum++)
            {
                var isLastPartition = (partNum == (partitionNum - 1));
                var args = new SelectQueryArguments<TInput, TOutput>()
                {
                    Selector = this.selector,
                    CollectionKey = this.Settings.CollectionKey,
                    StartIndex = (partNum * partitionSize),
                    StopIndex = isLastPartition ? collectionCount : ((partNum * partitionSize) + partitionSize)
                };

                threads[partNum] = DistributedThread.Create(func);
                threads[partNum].Start(args);
            }

        }

        [Serializable]
        private class SelectQueryArguments<TInput, TOutput>
        {
            public Func<TInput, TOutput> Selector { get; set; }

            public string CollectionKey { get; set; }

            public int StartIndex { get; set; }

            public int StopIndex { get; set; }
        }

        private class SelectQueryEnumerator : IEnumerator<TOutput>
        {
            public SelectQueryEnumerator()
            {

            }

            public TOutput Current
            {
                get { throw new NotImplementedException(); }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            object System.Collections.IEnumerator.Current
            {
                get { throw new NotImplementedException(); }
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
