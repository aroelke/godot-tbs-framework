using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.Extensions;

/// <summary>Structure defining the fields of a property for <see cref="GodotObject._GetPropertyList"/>.</summary>
/// <param name="Name">Name of the property.</param>
/// <param name="Type"><see cref="Variant"/> type of the property.</param>
/// <param name="Hint">String providing more information about the property's type.</param>
/// <param name="HintString">String providing specific information pertaining to the <paramref name="Hint"/>.</param>
/// <param name="Usage">Flags indicating usage of the property.</param>
public readonly record struct ObjectProperty(StringName Name, Variant.Type Type, PropertyHint Hint=PropertyHint.None, string HintString=null, PropertyUsageFlags Usage=PropertyUsageFlags.Default)
{
    public static implicit operator Godot.Collections.Dictionary(ObjectProperty property) => property.ToDictionary();

    /// <summary>Create a property that is restricted to a set of values.</summary>
    /// <typeparam name="T">Data type of the property values.</typeparam>
    /// <param name="name">Name of the property.</param>
    /// <param name="options">Values the property is restricted to.</param>
    public static ObjectProperty CreateEnumProperty<[MustBeVariant] T>(StringName name, IEnumerable<T> options) => new(
        name,
        Variant.From(options.First()).VariantType,
        PropertyHint.Enum,
        string.Join(",", options.Select((o) => o.ToString()))
    );

    public Godot.Collections.Dictionary ToDictionary() => new()
    {
        { "name", Name },
        { "type" , Variant.From(Type) },
        { "hint", Variant.From(Hint) },
        { "hint_string", HintString },
        { "usage", Variant.From(Usage) }
    };
}