using Godot;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Mapping of a particular game pad's name onto its button map.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMapElement : Resource
{
    /// <summary>Name of the game pad.</summary>
    [Export] public string GamepadName = "";

    /// <summary>Mapping of input actions onto the game pad's button icons.</summary>
    [Export] public IndividualGamepadButtonIconMap IconMap = null;
}