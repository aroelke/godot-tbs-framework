using System;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad buttons to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMap : IconMap
{
    /// <summary>
    /// Name of the file containing the general directional pad icon, without a direction pressed. Works with <c>Contains</c> and <c>this[]</c>.
    /// </summary>
    [Export] public string Dpad = "dpad";

    /// <param name="b">Game pad button to look for.</param>
    /// <returns>The icon mapped to the game pad button.</returns>
    public Texture2D this[JoyButton b] => this[Enum.GetName(b).ToLower()];

    /// <param name="b">Game pad button to test.</param>
    /// <returns><c>true</c> if the game pad button is mapped to an icon, and <c>false</c> otherwise.</returns>
    public bool Contains(JoyButton b) => Contains(Enum.GetName(b).ToLower());
}