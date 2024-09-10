using Godot;

namespace TbsTemplate.UI.Controls.Icons;

public abstract partial class IconMap : Resource, IIconMap
{
    public abstract Texture2D this[StringName action] { get; set; }
    public abstract bool ContainsKey(StringName action);
}