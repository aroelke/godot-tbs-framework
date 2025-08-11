using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class CompositeGamepadButtonIconMap : CompositeIconMap<JoyButton, GamepadButtonIconMap>, IGamepadButtonIconMap
{
    [Export] public override Godot.Collections.Dictionary<string, GamepadButtonIconMap> IconMaps { get; set; } = [];
    [Export] public override GamepadButtonIconMap NoMappingMap { get; set; } = null;

    public Texture2D Dpad
    {
        get => CurrentIconMap?.Dpad;
        set => WarnUseConstituents();
    }
}