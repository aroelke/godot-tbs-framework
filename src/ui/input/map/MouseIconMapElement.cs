using Godot;

namespace ui.input.map;

/// <summary>Element of a <c>MouseIconMap</c> used to populate it.</summary>
[GlobalClass, Tool]
public partial class MouseIconMapElement : Resource
{
    /// <summary>Mouse button to map to an icon.</summary>
    [Export] public MouseButton Button;

    /// <summary>Icon to display for the button.</summary>
    [Export] public Texture2D Icon;
}