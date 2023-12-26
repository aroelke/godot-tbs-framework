using Godot;

namespace ui.input.map;

/// <summary>Element of a <c>GamepadAxisIconMap</c> used to populate it.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMapElement : Resource
{
    /// <summary>Game pad axis to map to an icon.</summary>
    [Export] public JoyAxis Axis;

    /// <summary>Icon to display for the axis.</summary>
    [Export] public Texture2D Icon;
}