using Godot;

namespace ui.Device.Icons;

[GlobalClass, Tool]
public partial class GamepadAxisIconMapElement : Resource
{
    [Export] public string GamepadName = "";

    [Export] public IndividualGamepadAxisIconMap IconMap = null;
}