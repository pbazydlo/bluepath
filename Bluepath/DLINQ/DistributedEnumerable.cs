using Bluepath.DLINQ.Enumerables;
using Bluepath.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DLINQ
{
    public static class DistributedEnumerable
    {
        public static DistributedQuery<TSource> AsParallel<TSource>(this IEnumerable<TSource> source, IExtendedStorage storage)
            where TSource : new()
        {
            if(source == null)
            {
                throw new ArgumentNullException("source");
            }

            return new DistributedEnumerableWrapper<TSource>(source, storage);
        }

        //public static DistributedQuery<TResult> Select<TSource, TResult>(
        //    this DistributedQuery<TSource> source, Func<TSource, TResult> selector)
        //{
        //    if (source == null) throw new ArgumentNullException("source");
        //    if (selector == null) throw new ArgumentNullException("selector");
        //}
    }
}
