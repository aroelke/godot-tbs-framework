using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

/// <summary>Maps input action names to icons to display for them.</summary>
public abstract partial class IconMap : Resource, IReadOnlyDictionary<StringName, Texture2D>
{
    /// <summary>Icon to use when there's no icon mapped to the input action.</summary>
    [Export] public virtual Texture2D NoMappedActionIcon { get; set; } = null;

    public abstract IEnumerable<StringName> Keys { get; }
    public abstract IEnumerable<Texture2D> Values { get; }
    public abstract int Count { get; }

    public abstract Texture2D this[StringName key] { get; }

    public abstract bool ContainsKey(StringName key);
    public abstract IEnumerator<KeyValuePair<StringName, Texture2D>> GetEnumerator();
    public abstract bool TryGetValue(StringName key, [MaybeNullWhen(false)] out Texture2D value);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}