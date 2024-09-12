using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.Icons.Generic;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Resource mapping a specific gamepad's buttons to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class IndividualGamepadButtonIconMap : IndividualIconMap<JoyButton>
{
    public override Texture2D this[StringName action] { get => this[InputManager.GetInputGamepadButton(action)]; set => this[InputManager.GetInputGamepadButton(action)] = value; }

    /// <summary>Generic icon to display for the directional pad, with no directions pressed.</summary>
    [Export] public Texture2D Dpad = null;

    /// <summary>South action button icon.</summary>
    [Export] public Texture2D South
    {
        get => this[JoyButton.A];
        set => this[JoyButton.A] = value;
    }

    /// <summary>East action button icon.</summary>
    [Export] public Texture2D East
    {
        get => this[JoyButton.B];
        set => this[JoyButton.B] = value;
    }

    /// <summary>West action button icon.</summary>
    [Export] public Texture2D West
    {
        get => this[JoyButton.X];
        set => this[JoyButton.X] = value;
    }

    /// <summary>North action button icon.</summary>
    [Export] public Texture2D North
    {
        get => this[JoyButton.Y];
        set => this[JoyButton.Y] = value;
    }

    /// <summary>Directional pad up icon.</summary>
    [Export] public Texture2D DpadUp
    {
        get => this[JoyButton.DpadUp];
        set => this[JoyButton.DpadUp] = value;
    }

    /// <summary>Directional pad down icon.</summary>
    [Export] public Texture2D DpadDown
    {
        get => this[JoyButton.DpadDown];
        set => this[JoyButton.DpadDown] = value;
    }

    /// <summary>Directional pad left icon.</summary>
    [Export] public Texture2D DpadLeft
    {
        get => this[JoyButton.DpadLeft];
        set => this[JoyButton.DpadLeft] = value;
    }

    /// <summary>Directional pad right icon.</summary>
    [Export] public Texture2D DpadRight
    {
        get => this[JoyButton.DpadRight];
        set => this[JoyButton.DpadRight] = value;
    }

    /// <summary>Menu button (PS3- Start, PS4+ Options, Xbox Start, Nintendo +)
    [Export] public Texture2D Start
    {
        get => this[JoyButton.Start];
        set => this[JoyButton.Start] = value;
    }

    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputGamepadButton(action));
}