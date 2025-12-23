using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TbsFramework.Extensions;

namespace TbsFramework.MathExt;

/// <summary>Standalone methods for performing collection operations.</summary>
public static class Collections
{
    /// <summary>Find all permutations of some length from a collection.</summary>
    /// <param name="collection">Collection to permute.</param>
    /// <param name="length">Length of the permutations to find.</param>
    /// <returns>A collection containing all permutations of <paramref name="collection"/> of length <paramref name="length"/>.</returns>
    public static IEnumerable<IList<T>> Permutations<T>(IEnumerable<T> collection, int length)
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
    /// <param name="collection">Collection to permute.</param>
    /// <returns>A collection containing all permutations of <paramref name="collection"/>.</returns>
    public static IEnumerable<IList<T>> Permutations<T>(IEnumerable<T> collection) => collection.Permutations(collection.Count());

    /// <summary>Compute the cross product of two collections.</summary>
    /// <param name="a">First collection.</param>
    /// <param name="b">Second collection.</param>
    /// <returns>A new collection containing tuples whose elements are every pair of elements taken from <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static IEnumerable<(T, U)> Cross<T, U>(IEnumerable<T> a, IEnumerable<U> b)
    {
        List<(T, U)> head = [.. b.Select((e) => (a.First(), e))];
        if (a.Count() > 1)
            head.AddRange(Cross(a.Skip(1), b));
        return head;
    }
}