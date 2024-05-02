using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    /// <summary>Extensions for <see cref="IEnumerable{T}"/>.</summary>
    public static class IEnumerableExtensions
    {
        /// <summary>Sorts the elements of a sequence by using the specified comparison.</summary>
        /// <typeparam name="T">Type of the elements of the comparison.</typeparam>
        /// <typeparam name="U">Type of the key to use for sorting.</typeparam>
        /// <param name="collection">Sequence to sort.</param>
        /// <param name="keySelector">Function computing a key from an element of the sequence.</param>
        /// <param name="comparison">Comparison comparing the keys of two elements.</param>
        /// <returns>A sequence containing the elements of <paramref name="collection"/> sorted using keys computed by <paramref name="keySelector"/> with <paramref name="comparison"/>.</returns>
        public static IOrderedEnumerable<T> OrderBy<T, U>(this IEnumerable<T> collection, Func<T, U> keySelector, Comparison<U> comparison) => collection.OrderBy(keySelector, Comparer<U>.Create(comparison));

        /// <inheritdoc cref="OrderBy"/>
        /// <remarks><paramref name="comparison"/> operates directly on elements of <paramref name="collection"/> rather than using computed keys.</remarks>
        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> collection, Comparison<T> comparison) => collection.OrderBy((k) => k, Comparer<T>.Create(comparison));

        /// <summary>Project the elements of a collection of collections into a single, flat collection containing each one's elements.</summary>
        /// <typeparam name="T">Type of the elements of the collections.</typeparam>
        /// <returns>A collection containing all the constituent elements of the collections in <paramref name="collection"/>.</returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection) => collection.SelectMany((t) => t);

        /// <summary>Create a collection that cycles back to the first element after the last element is reached.</summary>
        /// <typeparam name="T">Type of the elements in <paramref name="collection"/></typeparam>
        /// <param name="collection">Collection of elements to convert.</param>
        /// <returns>A collection of items that repeats the elements of <paramref name="collection"/> indefinitely.</returns>
        public static IEnumerable<T> Cycle<T>(this IEnumerable<T> collection)
        {
            while (true)
                foreach (T t in collection)
                    yield return t;
        }

        /// <summary>Create an enumerator that iterates over a collection, but loops back around to the first element after reaching the last.</summary>
        /// <typeparam name="T">Type of elements in <paramref name="collection"/></typeparam>
        /// <param name="collection">Collection to iterate over.</param>
        /// <returns>An iterator over <paramref name="collection"/> that loops back to the beginning rather than ending.</returns>
        /// <remarks>Using this iterator in a <c>foreach</c> or <c>while</c> loop without a way to break out of it will result in an infinite loop.</remarks>
        public static IEnumerator<T> GetCyclicalEnumerator<T>(this IEnumerable<T> collection) => collection.Cycle().GetEnumerator();
    }
}