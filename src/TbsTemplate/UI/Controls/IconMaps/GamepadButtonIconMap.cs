using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

/// <summary>Maps gamepad button inputs to icons for a particular type of gamepad.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMap : GenericIconMap<JoyButton>, IGamepadButtonIconMap
{
    [Export] public override Godot.Collections.Dictionary<JoyButton, Texture2D> Icons { get; set; } = [];
    [Export] public Texture2D Dpad { get; set; } = null;

    public override JoyButton GetInput(StringName action) => InputManager.GetInputGamepadButton(action);
    public override bool InputIsInvalid(JoyButton input) => input == JoyButton.Invalid;
}