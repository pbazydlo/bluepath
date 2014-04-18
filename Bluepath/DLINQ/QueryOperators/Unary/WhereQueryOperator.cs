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
    public class WhereQueryOperator<TInputOutput> : UnaryQueryOperator<TInputOutput>
        //where TInputOutput : new()
    {
        private Func<TInputOutput, bool> predicate;

        public WhereQueryOperator(DistributedQuery<TInputOutput> query, Func<TInputOutput, bool> predicate)
            : base(query.Settings)
        {
            this.predicate = predicate;
        }

        protected override DistributedThread[] Execute()
        {
            //                    args,                                                             result key
            var func = new Func<UnaryQueryArguments<TInputOutput, bool>, IBluepathCommunicationFramework, byte[]>(
                (args, framework) =>
                {
                    if (!(framework.Storage is IExtendedStorage))
                    {
                        throw new ArgumentException("Provided storage must implement IExtendedStorage interface!");
                    }

                    var storage = framework.Storage as IExtendedStorage;
                    var initialCollection = new DistributedList<TInputOutput>(storage, args.CollectionKey);
                    List<TInputOutput> result = new List<TInputOutput>(args.StopIndex - args.StartIndex);
                    for (int i = args.StartIndex; i < args.StopIndex; i++)
                    {
                        if(args.QueryOperator(initialCollection[i]))
                        {
                            result.Add(initialCollection[i]);
                        }
                    }

                    return new UnaryQueryResult<TInputOutput>()
                    {
                        Result = result.ToArray()
                    }.Serialize();
                });

            var collectionToProcess = new DistributedList<TInputOutput>(this.Settings.Storage, this.Settings.CollectionKey);
            var collectionCount = collectionToProcess.Count;

            // TODO: Partition size should be calculated!
            var partitionSize = 10;
            var partitionNum = collectionCount / partitionSize;
            if (partitionNum == 0)
            {
                partitionNum = 1;
            }

            var minItemsPerPartition = collectionCount / partitionNum;

            DistributedThread[] threads
                = new DistributedThread[partitionNum];
            for (int partNum = 0; partNum < partitionNum; partNum++)
            {
                var isLastPartition = (partNum == (partitionNum - 1));
                var args = new UnaryQueryArguments<TInputOutput, bool>()
                {
                    QueryOperator = this.predicate,
                    CollectionKey = this.Settings.CollectionKey,
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
