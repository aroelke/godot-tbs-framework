using Godot;

namespace TbsTemplate.Extensions;

/// <summary>Structure defining the fields of a property for <see cref="GodotObject._GetPropertyList"/>.</summary>
/// <param name="Name">Name of the property.</param>
/// <param name="Type"><see cref="Variant"/> type of the property.</param>
/// <param name="Hint">String providing more information about the property's type.</param>
/// <param name="HintString">String providing specific information pertaining to the <paramref name="Hint"/>.</param>
public readonly record struct NodeProperty(StringName Name, Variant.Type Type, PropertyHint Hint=PropertyHint.None, string HintString=null)
{
    public static implicit operator Godot.Collections.Dictionary(NodeProperty property) => property.ToDictionary();

    public Godot.Collections.Dictionary ToDictionary() => new()
    {
        { "name", Name },
        { "type" , Variant.From(Type) },
        { "hint", Variant.From(Hint) },
        { "hint_string", HintString }
    };
}