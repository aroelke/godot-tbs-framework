using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad axes to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMap : Resource, IImmutableDictionary<JoyAxis, Texture2D>
{
    private IImmutableDictionary<JoyAxis, Texture2D> _map = null;
    private IImmutableDictionary<JoyAxis, Texture2D> Map => _map ??= Elements.ToImmutableDictionary((e) => e.Axis, (e) => e.Icon);

    /// <summary>Elements that make up the map.</summary>
    [Export] public GamepadAxisIconMapElement[] Elements = Array.Empty<GamepadAxisIconMapElement>();

    /// <summary>Icon for the general left stick icon, without a direction pressed.</summary>
    [Export] public Texture2D Left;

    /// <summary>Icon for the general right stick icon, without a direction pressed.</summary>
    [Export] public Texture2D Right;

    public Texture2D this[JoyAxis key] => Map[key];
    public IEnumerable<JoyAxis> Keys => Map.Keys;
    public IEnumerable<Texture2D> Values => Map.Values;
    public int Count => Map.Count;
    public IImmutableDictionary<JoyAxis, Texture2D> Add(JoyAxis key, Texture2D value) => Map.Add(key, value);
    public IImmutableDictionary<JoyAxis, Texture2D> AddRange(IEnumerable<KeyValuePair<JoyAxis, Texture2D>> pairs) => Map.AddRange(pairs);
    public IImmutableDictionary<JoyAxis, Texture2D> Clear() => Map.Clear();
    public bool Contains(KeyValuePair<JoyAxis, Texture2D> pair) => Map.Contains(pair);
    public bool ContainsKey(JoyAxis key) => Map.ContainsKey(key);
    public IEnumerator<KeyValuePair<JoyAxis, Texture2D>> GetEnumerator() => Map.GetEnumerator();
    public IImmutableDictionary<JoyAxis, Texture2D> Remove(JoyAxis key) => Map.Remove(key);
    public IImmutableDictionary<JoyAxis, Texture2D> RemoveRange(IEnumerable<JoyAxis> keys) => Map.RemoveRange(keys);
    public IImmutableDictionary<JoyAxis, Texture2D> SetItem(JoyAxis key, Texture2D value) => Map.SetItem(key, value);
    public IImmutableDictionary<JoyAxis, Texture2D> SetItems(IEnumerable<KeyValuePair<JoyAxis, Texture2D>> items) => Map.SetItems(items);
    public bool TryGetKey(JoyAxis equalKey, out JoyAxis actualKey) => Map.TryGetKey(equalKey, out actualKey);
    public bool TryGetValue(JoyAxis key, [MaybeNullWhen(false)] out Texture2D value) => Map.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => Map.GetEnumerator();
}