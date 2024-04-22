using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    /// <summary>Extensions for <see cref="IEnumerable{T}"/>.</summary>
    public static class IEnumerableExtensions
    {
        /// <summary>Project the elements of a collection of collections into a single, flat collection containing each one's elements.</summary>
        /// <typeparam name="T">Type of the elements of the collections.</typeparam>
        /// <returns>A collection containing all the constituent elements of the collections in <paramref name="collection"/>.</returns>
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection) => collection.SelectMany((t) => t);
    }
}