using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping mouse actions to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class MouseIconMap : IconMap
{
    private IEnumerable<StringName> _names = null;

    /// <summary>Icon to display indicating mouse motion.</summary>
    [Export] public Texture2D Motion = null;

    /// <param name="mb">Mouse button to get the icon for.</param>
    /// <returns>The icon corresponding to the mouse button to display.</returns>
    public Texture2D this[MouseButton mb] => Icons[Enum.GetName(mb)];

    public override IEnumerable<StringName> Names => _names ??= (Enum.GetValues(typeof(MouseButton)) as MouseButton[]).Select((i) => new StringName(Enum.GetName(i)));
}