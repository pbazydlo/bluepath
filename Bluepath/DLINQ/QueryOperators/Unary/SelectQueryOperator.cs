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

        private DistributedThread[] Execute()
        {
            //                    args,                                                             result key
            var func = new Func<SelectQueryArguments<TInput, TOutput>, IBluepathCommunicationFramework, byte[]>(
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
                        result[index] = args.Selector(initialCollection[i]);
                        index++;
                    }

                    return new SelectQueryResult<TOutput>()
                    {
                        Result = result
                    }.Serialize();
                });

            var collectionToProcess = new DistributedList<TInput>(this.Settings.Storage, this.Settings.CollectionKey);
            var collectionCount = collectionToProcess.Count;

            // TODO: Partition size should be calculated!
            var partitionSize = 10;
            var partitionNum = collectionCount / partitionSize;
            if(partitionNum==0)
            {
                partitionNum = 1;
            }

            var minItemsPerPartition = collectionCount / partitionNum;

            DistributedThread[] threads
                = new DistributedThread[partitionNum];
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

                if (this.Settings.DefaultConnectionManager != null)
                {
                    if (this.Settings.DefaultScheduler != null)
                    {
                        threads[partNum] = DistributedThread.Create(func, this.Settings.DefaultConnectionManager, this.Settings.DefaultScheduler);
                    }
                    else
                    {
                        threads[partNum] = DistributedThread.Create(func, this.Settings.DefaultConnectionManager, new ThreadNumberScheduler(this.Settings.DefaultConnectionManager));
                    }
                }
                else
                {
                    if (this.Settings.DefaultScheduler != null)
                    {
                        threads[partNum] = DistributedThread.Create(func, this.Settings.DefaultScheduler);
                    }
                    else
                    {
                        threads[partNum] = DistributedThread.Create(func);
                    }
                }

                threads[partNum].Start(args);
            }

            return threads;
        }

        public override IEnumerator<TOutput> GetEnumerator()
        {
            var threads = this.Execute();

            // TODO: It would be better if we knew what size result would be
            List<TOutput> temporaryResult = new List<TOutput>();
            foreach (var thread in threads)
            {
                thread.Join();

                // TODO: For some reason thread.Result gets deserialized even though it should stay byte[]
                // Tried implementing tests covering this case [DistributedThreadRemotelyExecutesStaticMethodTakingArrayAsParameterAndReturningArray],
                // however they seem to work differently
                var result = ((SelectQueryResult<TOutput>)thread.Result).Result;
                temporaryResult.AddRange(result);
            }

            var resultEnumerable
                = new DistributedEnumerableWrapper<TOutput>(
                    temporaryResult,
                    this.Settings.Storage,
                    this.Settings.DefaultConnectionManager,
                    this.Settings.DefaultScheduler
                    );
            return resultEnumerable.GetEnumerator();
        }

        [Serializable]
        private class SelectQueryArguments<TInput, TOutput>
        {
            public Func<TInput, TOutput> Selector { get; set; }

            public string CollectionKey { get; set; }

            public int StartIndex { get; set; }

            public int StopIndex { get; set; }
        }

        /// <summary>
        /// This class wraps processing result so that it can be safely transfered.
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        [Serializable]
        private class SelectQueryResult<TOutput>
        {
            public TOutput[] Result { get; set; }
        }
    }
}
