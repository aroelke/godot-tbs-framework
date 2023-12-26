using Godot;

namespace ui.input.map;

/// <summary>Element of a <c>KeyIconMap</c> used to populate it.</summary>
[GlobalClass, Tool]
public partial class KeyIconMapElement : Resource
{
    /// <summary>Keyboard key to map to an icon.</summary>
    [Export] public Key Key;

    /// <summary>Icon to display for the key.</summary>
    [Export] public Texture2D Icon;
}