using System.Collections.Generic;

namespace TbsFramework.Extensions;

/// <summary>Extensions for <see cref="IDictionary{TKey, TValue}"/>.</summary>
public static class IDictionaryExtensions
{
    /// <returns>The value associated with the key, or <paramref name="default"/> if there isn't one.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue @default=default) =>
        dictionary.TryGetValue(key, out TValue value) ? value : @default;
}