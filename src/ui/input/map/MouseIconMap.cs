using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping mouse actions to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class MouseIconMap : IconMap
{
    /// <summary>Name of the file containing the mouse-motion icon (without extension). Works with <c>Contains</c> and <c>this[]</c>.</summary>
    [Export] public string Motion = "mouse";

    public Texture2D this[MouseButton b] => this[Enum.GetName(b).ToLower()];

    public bool Contains(MouseButton b) => Contains(Enum.GetName(b).ToLower());
}