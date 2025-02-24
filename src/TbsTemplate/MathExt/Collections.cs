using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TbsTemplate.Extensions;

namespace TbsTemplate.MathExt;

/// <summary>Standalone methods for performing collection operations.</summary>
public static class Collections
{
    /// <summary>Find all permutations of some length from a collection.</summary>
    /// <typeparam name="T">Type of the elements in the collection.</typeparam>
    /// <param name="collection">Collection to permute.</param>
    /// <param name="length">Length of the permutations to find.</param>
    /// <returns>A collection containing all permutations of <paramref name="collection"/> of length <paramref name="length"/>.</returns>
    public static IEnumerable<IList<T>> Permutations<T>(this IEnumerable<T> collection, int length)
    {
        if (length > 1)
        {
            IImmutableList<T> immutable = [.. collection];
            return Enumerable.Range(0, immutable.Count).SelectMany((i) => immutable.Swap(0, i).Skip(1).Permutations(length - 1).Select<IList<T>, List<T>>((p) => [immutable[i], .. p]));
        }
        else
            return collection.Select((e) => new List<T>() { e });
    }

    /// <summary>Find all permutations of a collection.</summary>
    /// <typeparam name="T">Type of the elements of the collection.</typeparam>
    /// <param name="collection">Collection to permute.</param>
    /// <returns>A collection containing all permutations of <paramref name="collection"/>.</returns>
    public static IEnumerable<IList<T>> Permutations<T>(this IEnumerable<T> collection) => collection.Permutations(collection.Count());
}