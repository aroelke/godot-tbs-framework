using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad buttons to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMap : IconMap
{
    [Export] public string Dpad = "dpad";

    public Texture2D this[JoyButton b] => this[Enum.GetName(b).ToLower()];

    public bool Contains(JoyButton b) => Contains(Enum.GetName(b).ToLower());
}