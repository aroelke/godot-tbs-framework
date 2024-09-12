using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.UI.Controls.Icons.Generic;

/// <summary>Generic class defining icon maps for individual input devices.  <c>null</c> values are allowed, but don't count as defined.</summary>
/// <typeparam name="T"><c>enum</c> type defining inputs to map to icons.</typeparam>
public abstract partial class IndividualIconMap<T> : IconMap, IIconMap, IIconMap<T> where T : struct, Enum
{
    private readonly Dictionary<T, Texture2D> _icons = Enum.GetValues<T>().ToDictionary(static (k) => k, static _ => (Texture2D)null);
    private readonly Dictionary<StringName, T> _names = Enum.GetValues<T>().ToDictionary(static (k) => new StringName(Enum.GetName(k)), static (k) => k);

    /// <summary>Collection of inputs that have defined icons mapped to them.</summary>
    public IEnumerable<T> Keys => _icons.Keys.Where((k) => _icons[k] is not null);

    /// <summary>Collection of defined values in the mapping.</summary>
    public IEnumerable<Texture2D> Values => _icons.Values.Where((v) => v is not null);

    /// <summary>Number of icons in the mapping that are defined.</summary>
    public int Count => _icons.Count((p) => p.Value is not null);

    public Texture2D this[T key] { get => _icons[key]; set => _icons[key] = value; }
    public bool ContainsKey(T key) => _icons.ContainsKey(key);
}