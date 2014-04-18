using Bluepath.DLINQ.Enumerables;
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
                var result = ((UnaryQueryResult<TOutput>)thread.Result).Result;
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
        protected class UnaryQueryArguments<TInput, TOutput>
        {
            public Func<TInput, TOutput> QueryOperator { get; set; }

            public string CollectionKey { get; set; }

            public int StartIndex { get; set; }

            public int StopIndex { get; set; }
        }

        /// <summary>
        /// This class wraps processing result so that it can be safely transfered.
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        [Serializable]
        protected class UnaryQueryResult<TOutput>
        {
            public TOutput[] Result { get; set; }
        }
    }
}
