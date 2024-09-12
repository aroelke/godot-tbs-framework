using Godot;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Abstract resource defining a mapping of action names onto icons for them.</summary>
public abstract partial class IconMap : Resource, IIconMap
{
    /// <param name="action">Input action name.</param>
    /// <returns>The icon mapped to the input action, or <c>null</c> if there isn't one.</returns>
    public abstract Texture2D this[StringName action] { get; set; }

    /// <param name="action">Input action name.</param>
    /// <returns><c>true</c> if the input action has an icon defined, or <c>false</c>otherwise.</returns>
    public abstract bool ContainsKey(StringName action);
}