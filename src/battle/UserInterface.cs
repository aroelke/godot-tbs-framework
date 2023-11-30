using Godot;
using ui;

namespace battle;

public partial class UserInterface : CanvasLayer
{
    private CanvasItem _mouse = null, _keyboard = null, _playstation = null;

    private CanvasItem MouseControls => _mouse ??= GetNode<CanvasItem>("HUD/Mouse");
    private CanvasItem KeyboardControls => _keyboard ??= GetNode<CanvasItem>("HUD/Keyboard");
    private CanvasItem PlaystationControls => _playstation ??= GetNode<CanvasItem>("HUD/Playstation");

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            MouseControls.Visible = true;
            KeyboardControls.Visible = false;
            PlaystationControls.Visible = false;
            break;
        case InputEventKey:
            MouseControls.Visible = false;
            KeyboardControls.Visible = true;
            PlaystationControls.Visible = false;
            break;
        case InputEventJoypadButton:
            MouseControls.Visible = false;
            KeyboardControls.Visible = false;
            PlaystationControls.Visible = true;
            break;
        case InputEventJoypadMotion when VirtualMouse.GetAnalogVector() != Vector2.Zero:
            MouseControls.Visible = false;
            KeyboardControls.Visible = false;
            PlaystationControls.Visible = true;
            break;
        }
    }
}
