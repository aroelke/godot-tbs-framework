using Godot;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI.Controls.IconMaps;

/// <summary>Maps mouse inputs to icons to display for them.</summary>
[GlobalClass, Tool]
public partial class MouseIconMap : GenericIconMap<MouseButton>
{
    /// <summary>Icon to use to represent mouse motion.</summary>
    [Export] public Texture2D Motion = null;

    [Export] public override Godot.Collections.Dictionary<MouseButton, Texture2D> Icons { get; set; } = [];
    public override MouseButton GetInput(StringName action) => InputManager.GetInputMouseButton(action);
    public override bool InputIsInvalid(MouseButton input) => input == MouseButton.None;
}