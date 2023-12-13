using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad buttons to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadButtonIconMap : IconMap
{
    private IEnumerable<StringName> _names = null;

    /// <summary>Icon corresponding to the generic directional pad without any directions pressed.</summary>
    [Export] public Texture2D Dpad = null;

    /// <param name="b">Button to the the icon for.</param>
    /// <returns>The icon to display for the button.</returns>
    public Texture2D this[JoyButton b] => Icons[Enum.GetName(b)];

    /// <param name="b">Gamepad button to check.</param>
    /// <returns><c>true</c> if the gamepad button has been mapped to an icon, and <c>false</c> otherwise.</returns>
    public bool Contains(JoyButton b) => Contains(Enum.GetName(b));

    /// <returns><c>true</c> if the directional pad (no specific direction) has an icon mapped to it, and <c>false</c> otherwise.</returns>
    public bool ContainsDpad() => Dpad is not null;

    public override IEnumerable<StringName> Names => _names ??= (Enum.GetValues(typeof(JoyButton)) as JoyButton[]).Select((b) => new StringName(Enum.GetName(b)));
}