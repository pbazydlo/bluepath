using Bluepath.DLINQ.Enumerables;
using Bluepath.DLINQ.QueryOperators.Unary;
using Bluepath.Services;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ
{
    public static class DistributedEnumerable
    {
        public static DistributedQuery<TSource> AsDistributed<TSource>(
            this IEnumerable<TSource> source,
            IExtendedStorage storage,
            IConnectionManager connectionManager = null,
            IScheduler scheduler = null
            )
            where TSource : new()
        {
            if(source == null)
            {
                throw new ArgumentNullException("source");
            }

            return new DistributedEnumerableWrapper<TSource>(source, storage, connectionManager, scheduler);
        }

        public static DistributedQuery<TResult> Select<TSource, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, TResult> selector)
            where TSource : new()
            where TResult : new()
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");

            return new SelectQueryOperator<TSource, TResult>(source, selector);
        }

        public static DistributedQuery<TSource> Where<TSource>(
            this DistributedQuery<TSource> source, Func<TSource, bool> predicate)
            where TSource : new()
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            return new WhereQueryOperator<TSource>(source, predicate);
        }

        public static int Count<TSource>(
            this DistributedQuery<TSource> source, Func<TSource, bool> predicate)
            where TSource : new()
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            var whereResult = Where<TSource>(source, predicate);
            var collectionKey = whereResult.Settings.CollectionKey;
            var resultCollection = new DistributedList<TSource>(whereResult.Settings.Storage, collectionKey);

            return resultCollection.Count;
        }
    }
}
