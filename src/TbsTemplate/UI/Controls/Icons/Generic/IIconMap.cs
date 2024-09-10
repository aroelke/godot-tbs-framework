using System;
using Godot;

namespace TbsTemplate.UI.Controls.Icons.Generic;

/// <summary>Generic class declaring mappings of actions onto control icons.</summary>
public interface IIconMap<T> where T : struct, Enum
{
    public abstract Texture2D this[T key] { get; set; }

    public abstract bool ContainsKey(T key);
}