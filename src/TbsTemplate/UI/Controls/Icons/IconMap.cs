using Godot;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Generic class declaring mappings of actions onto control icons.</summary>
[GlobalClass, Tool]
public abstract partial class IconMap : Resource
{
    /// <param name="action">Action to get the icon for.</param>
    /// <returns>The icon mapped to the action. May be <c>null</c> if there is no mapped icon.</returns>
    public abstract Texture2D this[StringName action] { get; set; }

    /// <param name="action">Action to check.</param>
    /// <returns><c>true</c> if the action is mapped to a value, and <c>false</c> otherwise. May be <c>true</c> if the mapped value is <c>null</c>.</returns>
    public abstract bool ContainsKey(StringName action);
}