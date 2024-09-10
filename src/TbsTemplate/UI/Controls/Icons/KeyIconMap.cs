using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.Icons.Generic;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : IconMap, IIconMap<Key>
{
    private readonly Dictionary<Key, Texture2D> _icons = Enum.GetValues<Key>().ToDictionary(static (k) => k, static _ => (Texture2D)null);
    private readonly Dictionary<StringName, Key> _names = Enum.GetValues<Key>().ToDictionary(static (k) => new StringName(Enum.GetName(k)), static (k) => k);

    public ICollection<Key> Keys => _icons.Keys;
    public ICollection<Texture2D> Values => _icons.Values;
    public int Count => _icons.Count;
    public Texture2D this[Key key] { get => _icons[key]; set => _icons[key] = value; }
    public override Texture2D this[StringName action] { get => this[InputManager.GetInputKeycode(action)]; set => this[InputManager.GetInputKeycode(action)] = value; }

    /// <summary>Space bar icon.</summary>
    [Export] public Texture2D Space
    {
        get => _icons[Key.Space];
        set => _icons[Key.Space] = value;
    }

    /// <summary>Tab icon.</summary>
    [Export] public Texture2D Tab
    {
        get => _icons[Key.Tab];
        set => _icons[Key.Tab] = value;
    }

    /// <summary>Escape key icon.</summary>
    [Export] public Texture2D Escape
    {
        get => _icons[Key.Escape];
        set => _icons[Key.Escape] = value;
    }

    /// <summary>'A' key icon.</summary>
    [Export] public Texture2D A
    {
        get => _icons[Key.A];
        set => _icons[Key.A] = value;
    }

    /// <summary>'D' key icon.</summary>
    [Export] public Texture2D D
    {
        get => _icons[Key.D];
        set => _icons[Key.D] = value;
    }

    /// <summary>'S' key icon.</summary>
    [Export] public Texture2D S
    {
        get => _icons[Key.S];
        set => _icons[Key.S] = value;
    }

    /// <summary>'W' key icon.</summary>
    [Export] public Texture2D W
    {
        get => _icons[Key.W];
        set => _icons[Key.W] = value;
    }

    /// <summary>Left arrow key icon.</summary>
    [Export] public Texture2D Left
    {
        get => _icons[Key.Left];
        set => _icons[Key.Left] = value;
    }

    /// <summary>Up arrow key icon.</summary>
    [Export] public Texture2D Up
    {
        get => _icons[Key.Up];
        set => _icons[Key.Up] = value;
    }

    /// <summary>Right arrow key icon.</summary>
    [Export] public Texture2D Right
    {
        get => _icons[Key.Right];
        set => _icons[Key.Right] = value;
    }

    /// <summary>Down arrow key icon.</summary>
    [Export] public Texture2D Down
    {
        get => _icons[Key.Down];
        set => _icons[Key.Down] = value;
    }

    public bool ContainsKey(Key key) => _icons.ContainsKey(key);
    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputKeycode(action));
}