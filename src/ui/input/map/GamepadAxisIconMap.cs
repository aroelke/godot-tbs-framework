using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping game pad axes to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class GamepadAxisIconMap : IconMap
{
    private IEnumerable<StringName> _names = null;

    /// <summary>Icon to use for the left stick axis without any direction pressed.</summary>
    [Export] public Texture2D Left = null;

    /// <summary>Icon to use for the right stick axis without any direction pressed.</summary>
    [Export] public Texture2D Right = null;

    /// <param name="a">Axis to get the icon for.</param>
    /// <returns>The icon to display for the axis.</returns>
    public Texture2D this[JoyAxis a] => Icons[Enum.GetName(a)];

    /// <param name="a">Game pad axis to check.</param>
    /// <returns><c>true</c> if the game pad axis is mapped to an icon, and <c>false</c> otherwise.</returns>
    public bool Contains(JoyAxis a) => Contains(Enum.GetName(a));

    /// <returns><c>true</c> if the left stick axis (no direction pressed) is mapped to an icon, and <c>false</c> otherwise.</returns>
    public bool ContainsLeft() => Left is not null;

    /// <returns><c>true</c> if the right stick axis (no direction pressed) is mapped to an icon, and <c>false</c> otherwise.</returns>
    public bool ContainsRight() => Right is not null;

    public override IEnumerable<StringName> Names => _names ??= (Enum.GetValues(typeof(JoyAxis)) as JoyAxis[]).Select((a) => new StringName(Enum.GetName(a)));
}