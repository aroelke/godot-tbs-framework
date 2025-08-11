using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class CompositeGamepadAxisIconMap : CompositeIconMap<JoyAxis, GamepadAxisIconMap>
{
    [Export] public override Godot.Collections.Dictionary<string, GamepadAxisIconMap> IconMaps { get; set; } = [];
    [Export] public override GamepadAxisIconMap NoMappingMap { get; set; } = null;
}