using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class CompositeGamepadAxisIconMap : CompositeIconMap<JoyAxis, GamepadAxisIconMap>, IGamepadAxisIconMap
{
    [Export] public override Godot.Collections.Dictionary<string, GamepadAxisIconMap> IconMaps { get; set; } = [];
    [Export] public override GamepadAxisIconMap NoMappingMap { get; set; } = null;

    public Texture2D LeftAxis
    {
        get => CurrentIconMap?.LeftAxis;
        set => WarnUseConstituents();
    }

    public Texture2D RightAxis
    {
        get => CurrentIconMap?.RightAxis;
        set => WarnUseConstituents();
    }
}