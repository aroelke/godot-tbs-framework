using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class GamepadAxisIconMap : GenericIconMap<JoyAxis>, IGamepadAxisIconMap
{
    [Export] public override Godot.Collections.Dictionary<JoyAxis, Texture2D> Icons { get; set; } = [];
    [Export] public Texture2D LeftAxis { get; set; } = null;
    [Export] public Texture2D RightAxis { get; set; } = null;

    public override JoyAxis GetInput(StringName action) => InputManager.GetInputGamepadAxis(action);
    public override bool InputIsInvalid(JoyAxis input) => input == JoyAxis.Invalid;
}