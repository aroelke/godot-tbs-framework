using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad axes to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMap : IconMap
{
    [Export] public string Left = "left_stick";

    [Export] public string Right = "right_stick";

    public Texture2D this[JoyAxis a] => this[Enum.GetName(a).ToLower()];

    public bool Contains(JoyAxis a) => Contains(Enum.GetName(a).ToLower());
}