using Godot;

namespace ui.input.map;

/// <summary>Element of a <c>GamepadButtonIconMap</c> used to populate it.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMapElement : Resource
{
    /// <summary>Game pad button to map to an icon.</summary>
    [Export] public JoyButton Button;

    /// <summary>Icon to display for the button.</summary>
    [Export] public Texture2D Icon;
}