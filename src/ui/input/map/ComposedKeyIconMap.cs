using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class ComposedKeyIconMap : Resource, IImmutableDictionary<Key, Texture2D>
{
    private ImmutableDictionary<Key, Texture2D> _map = null;
    private ImmutableDictionary<Key, Texture2D> Map => _map ??= Elements.ToImmutableDictionary((e) => e.Key, (e) => e.Icon);

    /// <summary>Elements that make up the map.</summary>
    [Export] public KeyIconMapElement[] Elements = Array.Empty<KeyIconMapElement>();

    public Texture2D this[Key key] => Map[key];
    public IEnumerable<Key> Keys => Map.Keys;
    public IEnumerable<Texture2D> Values => Map.Values;
    public int Count => Map.Count;
    public IImmutableDictionary<Key, Texture2D> Add(Key key, Texture2D value) => Map.Add(key, value);
    public IImmutableDictionary<Key, Texture2D> AddRange(IEnumerable<KeyValuePair<Key, Texture2D>> pairs) => Map.AddRange(pairs);
    public IImmutableDictionary<Key, Texture2D> Clear() => Map.Clear();
    public bool Contains(KeyValuePair<Key, Texture2D> pair) => Map.Contains(pair);
    public bool ContainsKey(Key key) => Map.ContainsKey(key);
    public IEnumerator<KeyValuePair<Key, Texture2D>> GetEnumerator() => Map.GetEnumerator();
    public IImmutableDictionary<Key, Texture2D> Remove(Key key) => Map.Remove(key);
    public IImmutableDictionary<Key, Texture2D> RemoveRange(IEnumerable<Key> keys) => Map.RemoveRange(keys);
    public IImmutableDictionary<Key, Texture2D> SetItem(Key key, Texture2D value) => Map.SetItem(key, value);
    public IImmutableDictionary<Key, Texture2D> SetItems(IEnumerable<KeyValuePair<Key, Texture2D>> items) => Map.SetItems(items);
    public bool TryGetKey(Key equalKey, out Key actualKey) => Map.TryGetKey(equalKey, out actualKey);
    public bool TryGetValue(Key key, [MaybeNullWhen(false)] out Texture2D value) => Map.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => Map.GetEnumerator();
}