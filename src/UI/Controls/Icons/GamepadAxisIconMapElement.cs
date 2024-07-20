using Godot;

namespace UI.Controls.Icons;

/// <summary>Individual mapping of a game pad (by name) onto its button icon map.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMapElement : Resource
{
    /// <summary>Name of the game pad to map icons to.</summary>
    [Export] public string GamepadName = "";

    /// <summary>Mapping of input actions onto game pad button icons for the game pad.</summary>
    [Export] public IndividualGamepadAxisIconMap IconMap = null;
}