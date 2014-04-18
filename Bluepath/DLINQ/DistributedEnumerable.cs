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

            throw new NotImplementedException();
            //return new SelectManyQueryOperator<TSource, TResult, TResult>(source, selector, null, null);
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

            throw new NotImplementedException();
            //return new SelectManyQueryOperator<TSource, TResult, TResult>(source, null, selector, null);
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

            throw new NotImplementedException();
            //return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, collectionSelector, null, resultSelector);
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

            throw new NotImplementedException();
            //return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, null, collectionSelector, resultSelector);
        }
    }
}
