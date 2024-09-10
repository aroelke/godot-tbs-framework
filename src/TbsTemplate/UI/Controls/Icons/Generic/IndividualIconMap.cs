using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.UI.Controls.Icons.Generic;

public abstract partial class IndividualIconMap<T> : IconMap, IIconMap, IIconMap<T> where T : struct, Enum
{
    private readonly Dictionary<T, Texture2D> _icons = Enum.GetValues<T>().ToDictionary(static (k) => k, static _ => (Texture2D)null);
    private readonly Dictionary<StringName, T> _names = Enum.GetValues<T>().ToDictionary(static (k) => new StringName(Enum.GetName(k)), static (k) => k);

    public ICollection<T> Keys => _icons.Keys;
    public ICollection<Texture2D> Values => _icons.Values;
    public int Count => _icons.Count;
    public Texture2D this[T key] { get => _icons[key]; set => _icons[key] = value; }

    public bool ContainsKey(T key) => _icons.ContainsKey(key);
}