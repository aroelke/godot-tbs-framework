using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.Icons.Generic;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : IndividualIconMap<Key>
{
    public override Texture2D this[StringName action] { get => this[InputManager.GetInputKeycode(action)]; set => this[InputManager.GetInputKeycode(action)] = value; }

    /// <summary>Space bar icon.</summary>
    [Export] public Texture2D Space
    {
        get => this[Key.Space];
        set => this[Key.Space] = value;
    }

    /// <summary>Tab icon.</summary>
    [Export] public Texture2D Tab
    {
        get => this[Key.Tab];
        set => this[Key.Tab] = value;
    }

    /// <summary>Escape key icon.</summary>
    [Export] public Texture2D Escape
    {
        get => this[Key.Escape];
        set => this[Key.Escape] = value;
    }

    /// <summary>'A' key icon.</summary>
    [Export] public Texture2D A
    {
        get => this[Key.A];
        set => this[Key.A] = value;
    }

    /// <summary>'D' key icon.</summary>
    [Export] public Texture2D D
    {
        get => this[Key.D];
        set => this[Key.D] = value;
    }

    /// <summary>'S' key icon.</summary>
    [Export] public Texture2D S
    {
        get => this[Key.S];
        set => this[Key.S] = value;
    }

    /// <summary>'W' key icon.</summary>
    [Export] public Texture2D W
    {
        get => this[Key.W];
        set => this[Key.W] = value;
    }

    /// <summary>Left arrow key icon.</summary>
    [Export] public Texture2D Left
    {
        get => this[Key.Left];
        set => this[Key.Left] = value;
    }

    /// <summary>Up arrow key icon.</summary>
    [Export] public Texture2D Up
    {
        get => this[Key.Up];
        set => this[Key.Up] = value;
    }

    /// <summary>Right arrow key icon.</summary>
    [Export] public Texture2D Right
    {
        get => this[Key.Right];
        set => this[Key.Right] = value;
    }

    /// <summary>Down arrow key icon.</summary>
    [Export] public Texture2D Down
    {
        get => this[Key.Down];
        set => this[Key.Down] = value;
    }

    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputKeycode(action));
}