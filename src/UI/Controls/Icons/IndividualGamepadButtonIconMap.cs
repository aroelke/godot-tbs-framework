using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UI.Controls.Action;

namespace UI.Controls.Icons;

/// <summary>Resource mapping a specific gamepad's buttons to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class IndividualGamepadButtonIconMap : IconMap
{
    private readonly Dictionary<JoyButton, Texture2D> _icons = Enum.GetValues<JoyButton>().ToDictionary(static (k) => k, static _ => (Texture2D)null);
    private readonly Dictionary<StringName, JoyButton> _names = Enum.GetValues<JoyButton>().ToDictionary(static (k) => new StringName(Enum.GetName(k)), static (k) => k);

    public ICollection<JoyButton> Keys => _icons.Keys;
    public ICollection<Texture2D> Values => _icons.Values;
    public int Count => _icons.Count;
    public Texture2D this[JoyButton key] { get => _icons[key]; set => _icons[key] = value; }
    public override Texture2D this[InputActionReference action] { get => this[action.GamepadButton]; set => this[action.GamepadButton] = value; }

    /// <summary>Generic icon to display for the directional pad, with no directions pressed.</summary>
    [Export] public Texture2D Dpad = null;

    /// <summary>South action button icon.</summary>
    [Export] public Texture2D South
    {
        get => _icons[JoyButton.A];
        set => _icons[JoyButton.A] = value;
    }

    /// <summary>East action button icon.</summary>
    [Export] public Texture2D East
    {
        get => _icons[JoyButton.B];
        set => _icons[JoyButton.B] = value;
    }

    /// <summary>West action button icon.</summary>
    [Export] public Texture2D West
    {
        get => _icons[JoyButton.X];
        set => _icons[JoyButton.X] = value;
    }

    /// <summary>North action button icon.</summary>
    [Export] public Texture2D North
    {
        get => _icons[JoyButton.Y];
        set => _icons[JoyButton.Y] = value;
    }

    /// <summary>Directional pad up icon.</summary>
    [Export] public Texture2D DpadUp
    {
        get => _icons[JoyButton.DpadUp];
        set => _icons[JoyButton.DpadUp] = value;
    }

    /// <summary>Directional pad down icon.</summary>
    [Export] public Texture2D DpadDown
    {
        get => _icons[JoyButton.DpadDown];
        set => _icons[JoyButton.DpadDown] = value;
    }

    /// <summary>Directional pad left icon.</summary>
    [Export] public Texture2D DpadLeft
    {
        get => _icons[JoyButton.DpadLeft];
        set => _icons[JoyButton.DpadLeft] = value;
    }

    /// <summary>Directional pad right icon.</summary>
    [Export] public Texture2D DpadRight
    {
        get => _icons[JoyButton.DpadRight];
        set => _icons[JoyButton.DpadRight] = value;
    }

    /// <summary>Menu button (PS3- Start, PS4+ Options, Xbox Start, Nintendo +)
    [Export] public Texture2D Start
    {
        get => _icons[JoyButton.Start];
        set => _icons[JoyButton.Start] = value;
    }

    public bool ContainsKey(JoyButton key) => _icons.ContainsKey(key);
    public override bool ContainsKey(InputActionReference action) => ContainsKey(action.GamepadButton);
}