using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : IconMap
{
    public Texture2D this[Key k] => this[Enum.GetName(k).ToLower()];

    public bool Contains(Key k) => Contains(Enum.GetName(k).ToLower());
}