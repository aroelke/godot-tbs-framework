using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class KeyIconMap : GenericIconMap<Key>
{
    [Export] public override Godot.Collections.Dictionary<Key, Texture2D> Icons { get; set; } = [];
    public override Key GetInput(StringName action) => InputManager.GetInputKeycode(action);
    public override bool InputIsInvalid(Key input) => input == Key.None;
}