using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

public abstract partial class IconMap : Resource, IReadOnlyDictionary<StringName, Texture2D>
{
    public abstract IEnumerable<StringName> Keys { get; }
    public abstract IEnumerable<Texture2D> Values { get; }
    public abstract int Count { get; }

    public abstract Texture2D this[StringName key] { get; }

    public abstract bool ContainsKey(StringName key);
    public abstract IEnumerator<KeyValuePair<StringName, Texture2D>> GetEnumerator();
    public abstract bool TryGetValue(StringName key, [MaybeNullWhen(false)] out Texture2D value);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}