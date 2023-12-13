using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.input.map;

/// <summary>Resource representing a mapping from some set of values onto a set of <c>Texture2D</c> icons.</summary>
[Tool]
public abstract partial class IconMap : Resource
{
    /// <summary>Names used for representing the values in the property editor.</summary>
    public abstract IEnumerable<StringName> Names { get; }

    private Godot.Collections.Array<Godot.Collections.Dictionary> Properties = null;

    /// <summary>Mapping from value names to corresponding icons.</summary>
    protected readonly Dictionary<StringName, Texture2D> Icons = new();

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList() => Properties ??= new(Names.Select((s) => new Godot.Collections.Dictionary()
    {
        { "name", s },
        { "type", (int)Variant.Type.Object },
        { "hint", (int)PropertyHint.ResourceType },
        { "hint_string", "Texture2D" },
        { "usage", (int)(PropertyUsageFlags.Editor | PropertyUsageFlags.ScriptVariable) }
    }));

    public override Variant _Get(StringName property)
    {
        if (Icons.ContainsKey(property))
            return Icons[property];
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (Names.Contains(property))
        {
            if (value.Equals(default(Variant)) && Icons.ContainsKey(property))
                Icons.Remove(property);
            else
                Icons[property] = value.As<Texture2D>();
            return true;
        }
        return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property) => Icons.ContainsKey(property) || base._PropertyCanRevert(property);

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (Names.Contains(property))
            return default;
        return base._PropertyGetRevert(property);
    }
}