using System;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : IconMap
{
    /// <param name="k">Key code of the icon to get.</param>
    /// <returns>The icon corresponding to the key code.</returns>
    public Texture2D this[Key k] => this[Enum.GetName(k).ToLower()];

    /// <param name="k">Key code to test.</param>
    /// <returns><c>true</c> if there is an icon mapped to the key code, and <c>false</c> otherwise.</returns>
    public bool Contains(Key k) => Contains(Enum.GetName(k).ToLower());
}