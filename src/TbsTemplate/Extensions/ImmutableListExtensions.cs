using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TbsTemplate.Extensions;

/// <summary>Extensions for <see cref="IImmutableList{T}"/>.</summary>
public static class ImmutableListExtensions
{
    /// <summary>Remove all loops from a list of elements.  A loop is any sequence within the list that starts and ends with the same element.</summary>
    /// <typeparam name="T">Type of the elements of the list.</typeparam>
    /// <param name="items">List to straighten.</param>
    /// <returns>A new list containing all of the items in the input list with no loops.</returns>
    public static IImmutableList<T> Disentangle<T>(this IImmutableList<T> items)
    {
        for (int i = 0; i < items.Count; i++)
            for (int j = items.Count - 1; j > i; j--)
                if (EqualityComparer<T>.Default.Equals(items[i], items[j]))
                    return Disentangle(ImmutableList<T>.Empty.AddRange(items.Take(i)).AddRange(items.TakeLast(items.Count - j)));
        return items;
    }

    /// <summary>Swap two elements of a list.</summary>
    /// <typeparam name="T">Type of the elements of the list.</typeparam>
    /// <param name="items">List containing elements to be swapped.</param>
    /// <param name="i">Index of one of the elements to be swapped.</param>
    /// <param name="j">Index of one of the elements to be swapped.</param>
    /// <returns>A new list with the elements at indicies <paramref name="i"/> and <paramref name="j"/> swapped.</returns>
    public static IImmutableList<T> Swap<T>(this IImmutableList<T> items, int i, int j)
    {
        if (i == j)
            return items;

        T a = items[i];
        T b = items[j];
        return items.SetItem(i, b).SetItem(j, a);
    }
}