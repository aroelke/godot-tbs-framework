using System;
using Godot;

namespace TbsTemplate.UI.Controls.Icons.Generic;

/// <summary>Generic class declaring mappings of control inputs onto control icons.</summary>
/// <typeparam name="T"><c>enum</c> type defining the inputs to map to icons.</typeparam>
public interface IIconMap<T> where T : struct, Enum
{
    /// <param name="key">Control input.</param>
    /// <returns>The icon mapped to the control input, or <c>null</c> if there isn't one.</returns>
    public abstract Texture2D this[T key] { get; set; }

    /// <param name="key">Control input to check.</param>
    /// <returns><c>true</c> if the control input has an icon mapped to it, and <c>false</c> otherwise.</returns>
    public abstract bool ContainsKey(T key);
}