using System.Linq;
using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.HUD;

namespace TbsTemplate.Demo;

public partial class DemoMap : Node
{
    public void OnEnabledInputActionsUpdated(StringName[] actions)
    {
        foreach (ControlHint hint in GetNode("CanvasLayer/HUD/Hints").GetChildren().OfType<ControlHint>())
            hint.Visible = actions.Contains(hint.Get(ControlHint.PropertyName.Action).AsStringName());
        GetNode<CanvasItem>("CanvasLayer/HUD/Hints/CursorHint").Visible = actions.Intersect([
            InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveDown, InputManager.DigitalMoveRight,
            InputManager.AnalogMoveUp,  InputManager.AnalogMoveLeft,  InputManager.AnalogMoveDown,  InputManager.AnalogMoveRight
        ]).Any();
    }
}