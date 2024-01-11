using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad buttons to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class ComposedGamepadButtonIconMap : Resource, IImmutableDictionary<JoyButton, Texture2D>
{
    private ImmutableDictionary<JoyButton, Texture2D> _map = null;
    private ImmutableDictionary<JoyButton, Texture2D> Map => _map ??= Elements.ToImmutableDictionary((e) => e.Button, (e) => e.Icon);

    /// <summary>Elements that make up the map.</summary>
    [Export] public GamepadButtonIconMapElement[] Elements = Array.Empty<GamepadButtonIconMapElement>();

    /// <summary>Icon to show for the general directional pad icon, without a direction pressed.</summary>
    [Export] public Texture2D Dpad;

    public Texture2D this[JoyButton key] => Map[key];
    public IEnumerable<JoyButton> Keys => Map.Keys;
    public IEnumerable<Texture2D> Values => Map.Values;
    public int Count => Map.Count;
    public IImmutableDictionary<JoyButton, Texture2D> Add(JoyButton key, Texture2D value) => Map.Add(key, value);
    public IImmutableDictionary<JoyButton, Texture2D> AddRange(IEnumerable<KeyValuePair<JoyButton, Texture2D>> pairs) => Map.AddRange(pairs);
    public IImmutableDictionary<JoyButton, Texture2D> Clear() => Map.Clear();
    public bool Contains(KeyValuePair<JoyButton, Texture2D> pair) => Map.Contains(pair);
    public bool ContainsKey(JoyButton key) => Map.ContainsKey(key);
    public IEnumerator<KeyValuePair<JoyButton, Texture2D>> GetEnumerator() => Map.GetEnumerator();
    public IImmutableDictionary<JoyButton, Texture2D> Remove(JoyButton key) => Map.Remove(key);
    public IImmutableDictionary<JoyButton, Texture2D> RemoveRange(IEnumerable<JoyButton> keys) => Map.RemoveRange(keys);
    public IImmutableDictionary<JoyButton, Texture2D> SetItem(JoyButton key, Texture2D value) => Map.SetItem(key, value);
    public IImmutableDictionary<JoyButton, Texture2D> SetItems(IEnumerable<KeyValuePair<JoyButton, Texture2D>> items) => Map.SetItems(items);
    public bool TryGetKey(JoyButton equalKey, out JoyButton actualKey) => Map.TryGetKey(equalKey, out actualKey);
    public bool TryGetValue(JoyButton key, [MaybeNullWhen(false)] out Texture2D value) => Map.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => Map.GetEnumerator();
}