using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ui.Device.Icons;

/// <summary>Resource mapping keyboard keys onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : Resource
{
    private readonly Dictionary<Key, Texture2D> _icons = Enum.GetValues<Key>().ToDictionary((k) => k, _ => (Texture2D)null);
    private readonly Dictionary<StringName, Key> _names = Enum.GetValues<Key>().ToDictionary((k) => new StringName(Enum.GetName(k)), (k) => k);

    public ICollection<Key> Keys => _icons.Keys;
    public ICollection<Texture2D> Values => _icons.Values;
    public int Count => _icons.Count;
    public Texture2D this[Key key] { get => _icons[key]; set => _icons[key] = value; }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = new()
        {
            new ()
            {
                { "name", "KeyIconMap" },
                { "type", Variant.From(Variant.Type.Nil) },
                { "usage", Variant.From(PropertyUsageFlags.Category) }
            }
        };
        properties.AddRange(Enum.GetNames<Key>().Select((n) => new Godot.Collections.Dictionary()
        {
            { "name", n },
            { "type", Variant.From(Variant.Type.Object) },
            { "hint", Variant.From(PropertyHint.ResourceType) },
            { "hint_string", "Texture2D" }
        }));
        return properties;
    }

    public override Variant _Get(StringName property) => _names.ContainsKey(property) ? _icons[_names[property]] : default;

    public override bool _Set(StringName property, Variant value)
    {
        if (_names.ContainsKey(property))
        {
            _icons[_names[property]] = value.As<Texture2D>();
            return true;
        }
        else
            return false;
    }

    public bool ContainsKey(Key key) => _icons.ContainsKey(key);
}