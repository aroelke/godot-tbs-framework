using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.Icons.Generic;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Resource mapping a specific gamepad's axes to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class IndividualGamepadAxisIconMap : IndividualIconMap<JoyAxis>
{
    public override Texture2D this[StringName action] { get => this[InputManager.GetInputGamepadAxis(action)]; set => this[InputManager.GetInputGamepadAxis(action)] = value; }

    /// <summary>Generic icon to display for the left stick axis, not pressed in any direction.</summary>
    [Export] public Texture2D Left = null;

    /// <summary>Generic icon to display for the right stick axis, not pressed in any direction.</summary>
    [Export] public Texture2D Right = null;

    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputGamepadAxis(action));
}