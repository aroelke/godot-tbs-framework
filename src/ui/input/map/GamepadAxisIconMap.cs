using System;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad axes to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMap : IconMap
{
    /// <summary>
    /// Name of the file containing the general left stick icon, without a direction pressed. Works with <c>Contains</c> and <c>this[]</c>.
    /// </summary>
    [Export] public string Left = "left_stick";

    /// <summary>
    /// Name of the file containing the general right stick icon, without a direction pressed. Works with <c>Contains</c> and <c>this[]</c>.
    /// </summary>
    [Export] public string Right = "right_stick";

    /// <param name="a">Game pad axis to look for.</param>
    /// <returns>The icon mapped to the game pad axis.</returns>
    public Texture2D this[JoyAxis a] => this[Enum.GetName(a).ToLower()];

    /// <param name="a">Game pad axis to test.</param>
    /// <returns><c>true</c> if the game pad axis is mapped to an icon, and <c>false</c> otherwise.</returns>
    public bool Contains(JoyAxis a) => Contains(Enum.GetName(a).ToLower());
}