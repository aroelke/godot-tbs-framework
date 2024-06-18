using Godot;
using UI.Controls.Action;

namespace UI.Controls.Icons;

[GlobalClass, Tool]
public abstract partial class IconMap : Resource
{
    public abstract Texture2D this[InputActionReference action] { get; set; }

    public abstract bool ContainsKey(InputActionReference action);
}