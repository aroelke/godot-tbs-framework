using Godot;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.UI.Controls.IconMaps;

/// <summary>Maps keyboard key inputs to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class KeyIconMap : GenericIconMap<Key>
{
    [Export] public override Godot.Collections.Dictionary<Key, Texture2D> Icons { get; set; } = [];
    public override Key GetInput(StringName action) => InputManager.GetInputKeycode(action);
    public override bool InputIsInvalid(Key input) => input == Key.None;
}