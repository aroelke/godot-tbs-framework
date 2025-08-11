using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class CompositeGamepadButtonIconMap : CompositeIconMap<JoyButton, GamepadButtonIconMap>
{
    [Export] public override Godot.Collections.Dictionary<string, GamepadButtonIconMap> IconMaps { get; set; } = [];
    [Export] public override GamepadButtonIconMap NoMappingMap { get; set; } = null;
}