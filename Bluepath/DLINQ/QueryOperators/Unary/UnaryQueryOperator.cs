using Bluepath.DLINQ.Enumerables;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
using Bluepath.Threading.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ.QueryOperators.Unary
{
    public class UnaryQueryOperator<TOutput> : DistributedQuery<TOutput>
    //where TOutput : new()
    {
        protected UnaryQueryOperator(DistributedQuerySettings settings)
            : base(settings)
        {

        }

        protected virtual DistributedThread[] Execute()
        {
            throw new NotSupportedException();
        }

        protected DistributedThread CreateThread<TFunc>(TFunc func)
        {
            if (this.Settings.DefaultConnectionManager != null)
            {
                if (this.Settings.DefaultScheduler != null)
                {
                    return DistributedThread.Create(func, this.Settings.DefaultConnectionManager, this.Settings.DefaultScheduler);
                }
                else
                {
                    return DistributedThread.Create(func, this.Settings.DefaultConnectionManager, new ThreadNumberScheduler(this.Settings.DefaultConnectionManager));
                }
            }
            else
            {
                if (this.Settings.DefaultScheduler != null)
                {
                    return DistributedThread.Create(func, this.Settings.DefaultScheduler);
                }
                else
                {
                    return DistributedThread.Create(func);
                }
            }
        }

        protected void ExecuteAndJoin(out UnaryQueryResultCollectionType? collectionType, out string resultCollectionKey)
        {
            var threads = this.Execute();

            collectionType = null;
            resultCollectionKey = string.Empty;
            foreach (var thread in threads)
            {
                thread.Join();

                // TODO: For some reason thread.Result gets deserialized even though it should stay byte[]
                // Tried implementing tests covering this case [DistributedThreadRemotelyExecutesStaticMethodTakingArrayAsParameterAndReturningArray],
                // however they seem to work differently
                var result = (UnaryQueryResult)thread.Result;
                if (!collectionType.HasValue)
                {
                    collectionType = result.CollectionType;
                }

                if (resultCollectionKey == string.Empty)
                {
                    resultCollectionKey = result.CollectionKey;
                }
            }
        }

        public override IEnumerator<TOutput> GetEnumerator()
        {
            UnaryQueryResultCollectionType? collectionType;
            string resultCollectionKey;
            ExecuteAndJoin(out collectionType, out resultCollectionKey);

            if (collectionType.Value == UnaryQueryResultCollectionType.DistributedList)
            {
                var result = new DistributedList<TOutput>(this.Settings.Storage, resultCollectionKey);
                return new DistributedEnumerableWrapper<TOutput>(
                    result,
                    this.Settings.Storage,
                    this.Settings.DefaultConnectionManager,
                    this.Settings.DefaultScheduler
                    ).GetEnumerator();
            }
            else
            {
                // as we don't have enough data to create distributed dictionary we can't do this here
                throw new NotSupportedException();
            }
        }

        [Serializable]
        protected class UnaryQueryArguments<TIn, TOut>
        {
            public Func<TIn, TOut> QueryOperator { get; set; }

            public string CollectionKey { get; set; }

            public string ResultCollectionKey { get; set; }

            public int StartIndex { get; set; }

            public int StopIndex { get; set; }
        }

        /// <summary>
        /// This class wraps processing result so that it can be safely transfered.
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        [Serializable]
        protected class UnaryQueryResult
        {
            /// <summary>
            /// Specifies type of collection used to store result - needs to be the SAME for all results.
            /// </summary>
            public UnaryQueryResultCollectionType CollectionType { get; set; }

            /// <summary>
            /// Specifies key of collection used to store result - needs to be the SAME for all results.
            /// </summary>
            public string CollectionKey { get; set; }
        }

        /// <summary>
        /// Specifies collection type used to store result
        /// </summary>
        protected enum UnaryQueryResultCollectionType
        {
            DistributedList,
            DistributedDictionary
        }
    }
}
