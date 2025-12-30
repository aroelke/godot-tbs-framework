using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TbsFramework.Collections;

/// <summary>A generic collection of key/value pairs that raises events when contents change.</summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
{
    /// <summary>Handles events related to items being added or removed from the dictionary.</summary>
    /// <param name="updates">Key/value pairs that were added or removed.</param>
    public delegate void ItemsChangedEventHandler(IEnumerable<KeyValuePair<TKey, TValue>> updates);

    /// <summary>Handles events related to the value of a key in the dictionary being changed.</summary>
    /// <param name="key">Key being updated.</param>
    /// <param name="old">Old value for the key before it was updated.</param>
    /// <param name="new">Current value for the key after the update.</param>
    public delegate void ItemReplacedEventHandler(TKey key, TValue old, TValue @new);

    private static readonly object Sync = new();

    private readonly Dictionary<TKey, TValue> _backend = [];

    /// <summary>Event raised when items are added to the dictionary.</summary>
    public event ItemsChangedEventHandler ItemsAdded;
    /// <summary>Event raised when items are removed from the dictionary.</summary>
    public event ItemsChangedEventHandler ItemsRemoved;
    /// <summary>Event raised when the value associated with a key in the dictionary is changed.</summary>
    public event ItemReplacedEventHandler ItemReplaced;

    public ICollection<TKey> Keys => _backend.Keys;
    public ICollection<TValue> Values => _backend.Values;
    public int Count => _backend.Count;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => Sync;
    ICollection IDictionary.Keys => _backend.Keys;
    ICollection IDictionary.Values => _backend.Values;

    public TValue this[TKey key]
    {
        get => _backend[key];
        set
        {
            if (_backend.TryGetValue(key, out TValue v))
            {
                if (!EqualityComparer<TValue>.Default.Equals(v, value))
                {
                    TValue old = _backend[key];
                    _backend[key] = value;
                    if (ItemReplaced is not null)
                        ItemReplaced(key, old, value);
                }
            }
            else
            {
                _backend[key] = value;
                if (ItemsAdded is not null)
                    ItemsAdded([new(key, value)]);
            }
        }
    }

    public object this[object key]
    {
        get => this[(TKey)key];
        set => this[(TKey)key] = (TValue)value;
    }

    public void Add(TKey key, TValue value)
    {
        _backend.Add(key, value);
        if (ItemsAdded is not null)
            ItemsAdded([new(key, value)]);
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Add(object key, object value) => Add((TKey)key, (TValue)value);

    public bool Remove(TKey key)
    {
        if (_backend.TryGetValue(key, out TValue value))
        {
            _backend.Remove(key);
            if (ItemsRemoved is not null)
                ItemsRemoved([new(key, value)]);
            return true;
        }
        else
            return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => _backend.Contains(item) && Remove(item.Key);

    public void Remove(object key)
    {
        if (key is TKey k)
            Remove(k);
    }

    public void Clear()
    {
        List<KeyValuePair<TKey, TValue>> items = [.. _backend];
        _backend.Clear();
        ItemsRemoved(items);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => _backend.Contains(item);
    public bool Contains(object key) => key is TKey k && ContainsKey(k);
    public bool ContainsKey(TKey key) => _backend.ContainsKey(key);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _backend.TryGetValue(key, out value);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => (_backend as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
    public void CopyTo(Array array, int index) => (_backend as IDictionary).CopyTo(array, index);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _backend.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _backend.GetEnumerator();
    IDictionaryEnumerator IDictionary.GetEnumerator() => _backend.GetEnumerator();
}