using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping mouse actions to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class ComposedMouseIconMap : Resource, IImmutableDictionary<MouseButton, Texture2D>
{
    private ImmutableDictionary<MouseButton, Texture2D> _map = null;
    private ImmutableDictionary<MouseButton, Texture2D> Map => _map ??= Elements.ToImmutableDictionary((e) => e.Button, (e) => e.Icon);

    /// <summary>Elements making up the mapping of mouse buttons on to display icons.</summary>
    [Export] public MouseIconMapElement[] Elements = Array.Empty<MouseIconMapElement>();

    /// <summary>Icon to show for mouse motion, which isn't mapped to a specific button.</summary>
    [Export] public Texture2D Motion;

    public IEnumerable<MouseButton> Keys => Map.Keys;
    public IEnumerable<Texture2D> Values => Map.Values;
    public int Count => Map.Count;
    public Texture2D this[MouseButton key] => Map[key];
    public IImmutableDictionary<MouseButton, Texture2D> Add(MouseButton key, Texture2D value) => Map.Add(key, value);
    public IImmutableDictionary<MouseButton, Texture2D> AddRange(IEnumerable<KeyValuePair<MouseButton, Texture2D>> pairs) => Map.AddRange(pairs);
    public IImmutableDictionary<MouseButton, Texture2D> Clear() => Map.Clear();
    public bool Contains(KeyValuePair<MouseButton, Texture2D> pair) => Map.Contains(pair);
    public IImmutableDictionary<MouseButton, Texture2D> Remove(MouseButton key) => Map.Remove(key);
    public IImmutableDictionary<MouseButton, Texture2D> RemoveRange(IEnumerable<MouseButton> keys) => Map.RemoveRange(keys);
    public IImmutableDictionary<MouseButton, Texture2D> SetItem(MouseButton key, Texture2D value) => Map.SetItem(key, value);
    public IImmutableDictionary<MouseButton, Texture2D> SetItems(IEnumerable<KeyValuePair<MouseButton, Texture2D>> items) => Map.SetItems(items);
    public bool TryGetKey(MouseButton equalKey, out MouseButton actualKey) => Map.TryGetKey(equalKey, out actualKey);
    public bool ContainsKey(MouseButton key) => Map.ContainsKey(key);
    public bool TryGetValue(MouseButton key, [MaybeNullWhen(false)] out Texture2D value) => Map.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<MouseButton, Texture2D>> GetEnumerator() => Map.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Map.GetEnumerator();
}