using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

[GlobalClass, Tool]
public partial class MouseIconMap : GenericIconMap<MouseButton>
{
    [Export] public Texture2D Motion = null;

    [Export] public override Godot.Collections.Dictionary<MouseButton, Texture2D> Icons { get; set; } = [];
    public override MouseButton GetInput(StringName action) => InputManager.GetInputMouseButton(action);
    public override bool InputIsInvalid(MouseButton input) => input == MouseButton.None;
}