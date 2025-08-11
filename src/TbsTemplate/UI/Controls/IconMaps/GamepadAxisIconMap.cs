using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class GamepadAxisIconMap : GenericIconMap<JoyAxis>
{
    [Export] public override Godot.Collections.Dictionary<JoyAxis, Texture2D> Icons { get; set; } = [];
    public override JoyAxis GetInput(StringName action) => InputManager.GetInputGamepadAxis(action);
    public override bool InputIsInvalid(JoyAxis input) => input == JoyAxis.Invalid;
}