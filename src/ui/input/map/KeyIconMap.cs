using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : IconMap
{
    private IEnumerable<StringName> _names = null;

    /// <param name="k">Key to search for an icon.</param>
    /// <returns>The icon to display corresponding to the key</returns>
    public Texture2D this[Key k] => Icons[Enum.GetName(k)];

    public override IEnumerable<StringName> Names => _names ??= (Enum.GetValues(typeof(Key)) as Key[]).Select((i) => new StringName(Enum.GetName(i)));
}