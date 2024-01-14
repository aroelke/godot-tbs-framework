using Godot;

namespace ui.Device.Icons;

[GlobalClass, Tool]
public partial class GamepadButtonIconMapElement : Resource
{
    [Export] public string GamepadName = "";

    [Export] public IndividualGamepadButtonIconMap IconMap = null;
}