using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

/// <summary>Adds input enum mappings to display icons in addition to input action names.</summary>
/// <typeparam name="T">Type of input enum being mapped to display icons.</typeparam>
public abstract partial class GenericIconMap<[MustBeVariant] T> : IconMap, IReadOnlyDictionary<T, Texture2D> where T : struct, Enum
{
    /// <summary>Mapping from input values to icons.</summary>
    public abstract Godot.Collections.Dictionary<T, Texture2D> Icons { get; set; }

    /// <summary>Icon to use when there's no icon mapped to an input.</summary>
    [Export] public virtual Texture2D NoMappedInputIcon { get; set; } = null;

    public override IEnumerable<StringName> Keys => Icons.Keys.Select((k) => new StringName(Enum.GetName(k)));
    public override IEnumerable<Texture2D> Values => Icons.Values;
    public override int Count => Icons.Count;
    IEnumerable<T> IReadOnlyDictionary<T, Texture2D>.Keys => Icons.Keys;

    public Texture2D this[T key] => Icons[key];

    public override Texture2D this[StringName key]
    {
        get
        {
            T input = GetInput(key);
            if (InputIsInvalid(input))
                return NoMappedActionIcon;
            else if (Icons.TryGetValue(input, out Texture2D icon))
                return icon;
            else
                return NoMappedInputIcon;
        }
    }

    public abstract T GetInput(StringName action);
    public abstract bool InputIsInvalid(T input);

    public bool ContainsKey(T key) => Icons.ContainsKey(key);
    public override IEnumerator<KeyValuePair<StringName, Texture2D>> GetEnumerator() => Icons.ToDictionary((e) => new StringName(Enum.GetName(e.Key)), (e) => e.Value).GetEnumerator();
    IEnumerator<KeyValuePair<T, Texture2D>> IEnumerable<KeyValuePair<T, Texture2D>>.GetEnumerator() => Icons.GetEnumerator();
    public bool TryGetValue(T key, [MaybeNullWhen(false)] out Texture2D value) => Icons.TryGetValue(key, out value);

    public override bool ContainsKey(StringName key)
    {
        T input = GetInput(key);
        return !InputIsInvalid(input) && Icons.ContainsKey(input);
    }

    public override bool TryGetValue(StringName key, [MaybeNullWhen(false)] out Texture2D value)
    {
        T input = GetInput(key);
        if (InputIsInvalid(input))
        {
            value = null;
            return false;
        }
        else
            return Icons.TryGetValue(input, out value);
    }
}