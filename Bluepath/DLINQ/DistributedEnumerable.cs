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
        //where TSource : new()
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return new DistributedEnumerableWrapper<TSource>(source, storage, connectionManager, scheduler);
        }

        public static DistributedQuery<TResult> Select<TSource, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, TResult> selector)
        //where TSource : new()
        /*where TResult : new()*/
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            if (!selector.Method.IsStatic) throw new ArgumentException("Selector needs to be a static function.", "selector");

            return new SelectQueryOperator<TSource, TResult>(source, selector);
        }

        public static DistributedQuery<TSource> Where<TSource>(
            this DistributedQuery<TSource> source, Func<TSource, bool> predicate)
        //where TSource : new()
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");
            if (!predicate.Method.IsStatic) throw new ArgumentException("Predicate needs to be a static function.", "predicate");

            return new WhereQueryOperator<TSource>(source, predicate);
        }

        public static int Count<TSource>(
            this DistributedQuery<TSource> source, Func<TSource, bool> predicate)
        //where TSource : new()
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");
            if (!predicate.Method.IsStatic) throw new ArgumentException("Predicate needs to be a static function.", "predicate");

            var whereResult = Where<TSource>(source, predicate);
            var collectionKey = whereResult.Settings.CollectionKey;
            var resultCollection = new DistributedList<TSource>(whereResult.Settings.Storage, collectionKey);

            return resultCollection.Count;
        }

        /// <summary>
        /// Projects in parallel each element of a sequence to an IEnumerable{T} 
        /// and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <B>selector</B>.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform 
        /// function on each element of the input sequence.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> SelectMany<TSource, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            if (!selector.Method.IsStatic) throw new ArgumentException("Selector needs to be a static function.", "selector");

            return new SelectManyQueryOperator<TSource, TResult, TResult>(source, selector, null, null);
        }

        /// <summary>
        /// Projects in parallel each element of a sequence to an IEnumerable{T}, and flattens the resulting 
        /// sequences into one sequence. The index of each source element is used in the projected form of 
        /// that element.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by <B>selector</B>.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform 
        /// function on each element of the input sequence.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="selector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> SelectMany<TSource, TResult>(
             this DistributedQuery<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            if (!selector.Method.IsStatic) throw new ArgumentException("Selector needs to be a static function.", "selector");

            return new SelectManyQueryOperator<TSource, TResult, TResult>(source, null, selector, null);
        }

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable{T}, 
        /// flattens the resulting sequences into one sequence, and invokes a result selector 
        /// function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each source element; 
        /// the second parameter of the function represents the index of the source element.</param>
        /// <param name="resultSelector">A function to create a result element from an element from 
        /// the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>A sequence whose elements are the result of invoking the one-to-many transform 
        /// function <paramref name="collectionSelector"/> on each element of <paramref name="source"/> and then mapping 
        /// each of those sequence elements and their corresponding source element to a result element.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="collectionSelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> SelectMany<TSource, TCollection, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (collectionSelector == null) throw new ArgumentNullException("collectionSelector");
            if (!collectionSelector.Method.IsStatic) throw new ArgumentException("CollectionSelector needs to be a static function.", "collectionSelector");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            if (!resultSelector.Method.IsStatic) throw new ArgumentException("ResultSelector needs to be a static function.", "resultSelector");

            return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, collectionSelector, null, resultSelector);
        }

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable{T}, flattens the resulting 
        /// sequences into one sequence, and invokes a result selector function on each element 
        /// therein. The index of each source element is used in the intermediate projected 
        /// form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by 
        /// <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of elements to return.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each source element; 
        /// the second parameter of the function represents the index of the source element.</param>
        /// <param name="resultSelector">A function to create a result element from an element from 
        /// the first sequence and a collection of matching elements from the second sequence.</param>
        /// <returns>
        /// A sequence whose elements are the result of invoking the one-to-many transform 
        /// function <paramref name="collectionSelector"/> on each element of <paramref name="source"/> and then mapping 
        /// each of those sequence elements and their corresponding source element to a 
        /// result element.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="collectionSelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> SelectMany<TSource, TCollection, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (collectionSelector == null) throw new ArgumentNullException("collectionSelector");
            if (!collectionSelector.Method.IsStatic) throw new ArgumentException("CollectionSelector needs to be a static function.", "collectionSelector");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            if (!resultSelector.Method.IsStatic) throw new ArgumentException("ResultSelector needs to be a static function.", "resultSelector");

            return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, null, collectionSelector, resultSelector);
        }

        /// <summary>
        /// Distributely groups the elements of a sequence according to a specified key 
        /// selector function and creates a result value from each group and its key. 
        /// The elements of each group are projected by using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each 
        /// IGrouping{TKey, TElement}.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an 
        /// IGrouping&lt;TKey, TElement&gt;.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>A collection of elements of type <typeparamref name="TElement"/> where each element represents a 
        /// projection over a group and its key.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");

            return source.GroupBy<TSource, TKey, TElement>(keySelector, elementSelector)
                .Select<IGrouping<TKey, TElement>, TResult>(delegate(IGrouping<TKey, TElement> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function and 
        /// creates a result value from each group and its key. Key values are compared by using a 
        /// specified comparer, and the elements of each group are projected by using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in each 
        /// IGrouping{TKey, TElement}.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an 
        /// IGrouping{Key, TElement}.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An IEqualityComparer{TKey} to compare keys.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a 
        /// projection over a group and its key.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> or <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");

            return source.GroupBy<TSource, TKey, TElement>(keySelector, elementSelector, comparer)
                .Select<IGrouping<TKey, TElement>, TResult>(delegate(IGrouping<TKey, TElement> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Distributely groups the elements of a sequence according to a specified key selector function and 
        /// projects the elements for each group by using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the IGrouping</typeparam>
        /// <param name="source">An OrderedParallelQuery&lt;(Of &lt;(TElement&gt;)&gt;) than contains 
        /// elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an  IGrouping.</param>
        /// <returns>A ParallelQuery&lt;IGrouping&lt;TKey, TElement&gt;&gt; in C# or 
        /// ParallelQuery(Of IGrouping(Of TKey, TElement)) in Visual Basic where each IGrouping 
        /// generic object contains a collection of objects of type <typeparamref name="TElement"/> and a key.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return GroupBy<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
        }

        /// <summary>
        /// Distributely groups the elements of a sequence according to a key selector function. 
        /// The keys are compared by using a comparer and each group's elements are projected by 
        /// using a specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the IGrouping</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource}than contains elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="elementSelector">A function to map each source element to an element in an  IGrouping.</param>
        /// <param name="comparer">An IComparer{TSource} to compare keys.</param>
        /// <returns>
        /// A ParallelQuery{IGrouping{TKey, TElement}} in C# or 
        /// ParallelQuery(Of IGrouping(Of TKey, TElement)) in Visual Basic where each IGrouping 
        /// generic object contains a collection of objects of type <typeparamref name="TElement"/> and a key.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="elementSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (keySelector == null) throw new ArgumentNullException("keySelector");
            if (elementSelector == null) throw new ArgumentNullException("elementSelector");

            throw new NotImplementedException();
            //return new GroupByQueryOperator<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified 
        /// key selector function and creates a result value from each group and its key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult"/> where each element represents a 
        /// projection over a group and its key.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> GroupBy<TSource, TKey, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");

            return source.GroupBy<TSource, TKey>(keySelector)
                .Select<IGrouping<TKey, TSource>, TResult>(delegate(IGrouping<TKey, TSource> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function 
        /// and creates a result value from each group and its key. The keys are compared 
        /// by using a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="resultSelector">A function to create a result value from each group.</param>
        /// <param name="comparer">An IEqualityComparer{TKey} to compare keys.</param>
        /// <returns>
        /// An <B>ParallelQuery&lt;IGrouping&lt;TKey, TResult&gt;&gt;</B> in C# or 
        /// <B>ParallelQuery(Of IGrouping(Of TKey, TResult))</B> in Visual Basic where each 
        /// IGrouping&lt;(Of &lt;(TKey, TResult&gt;)&gt;) object contains a collection of objects 
        /// of type <typeparamref name="TResult"/> and a key.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> or
        /// <paramref name="resultSelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<TResult> GroupBy<TSource, TKey, TResult>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");

            return source.GroupBy<TSource, TKey>(keySelector, comparer).Select<IGrouping<TKey, TSource>, TResult>(
                delegate(IGrouping<TKey, TSource> grouping) { return resultSelector(grouping.Key, grouping); });
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource}than contains 
        /// elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>An OrderedParallelQuery{TSource}whose elements are sorted 
        /// descending according to a key.</returns>
        public static DistributedQuery<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector)
        {
            return GroupBy<TSource, TKey>(source, keySelector, null);
        }

        /// <summary>
        /// Groups in parallel the elements of a sequence according to a specified key selector function and compares the keys by using a specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>>.</typeparam>
        /// <param name="source">An OrderedParallelQuery{TSource} than contains 
        /// elements to sort.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer{TSource} to compare keys.</param>
        /// <returns>An OrderedParallelQuery{TSource} whose elements are sorted 
        /// descending according to a key.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public static DistributedQuery<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this DistributedQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (keySelector == null) throw new ArgumentNullException("keySelector");

            throw new NotImplementedException();
            //return new GroupByQueryOperator<TSource, TKey, TSource>(source, keySelector, null, comparer);
        }
    }
}
