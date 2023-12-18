using System;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping mouse actions to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class MouseIconMap : IconMap
{
    /// <summary>Name of the file containing the mouse-motion icon (without extension). Works with <c>Contains</c> and <c>this[]</c>.</summary>
    [Export] public string Motion = "mouse";

    /// <param name="b">Mouse button to look for.</param>
    /// <returns>The icon mapped to the mouse button.</returns>
    public Texture2D this[MouseButton b] => this[Enum.GetName(b).ToLower()];

    /// <param name="b">Mouse button to test.</param>
    /// <returns><c>true</c> if there is an icon mapped to the mouse button, and <c>false</c> otherwise.</returns>
    public bool Contains(MouseButton b) => Contains(Enum.GetName(b).ToLower());
}