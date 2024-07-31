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
}