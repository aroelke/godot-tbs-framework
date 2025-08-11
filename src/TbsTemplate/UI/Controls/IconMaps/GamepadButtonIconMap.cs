using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class GamepadButtonIconMap : GenericIconMap<JoyButton>
{
    [Export] public override Godot.Collections.Dictionary<JoyButton, Texture2D> Icons { get; set; } = [];
    public override JoyButton GetInput(StringName action) => InputManager.GetInputGamepadButton(action);
    public override bool InputIsInvalid(JoyButton input) => input == JoyButton.Invalid;
}