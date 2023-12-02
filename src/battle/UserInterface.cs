using System.Collections.Generic;
using Godot;
using ui;

namespace battle;

public enum ControlType
{
    Mouse,
    Keyboard,
    Playstation
}

public partial class UserInterface : CanvasLayer
{
    private CanvasItem _mouse = null, _keyboard = null, _playstation = null;
    private ControlType _controlType = ControlType.Mouse;

    private Dictionary<ControlType, CanvasItem> _hints = new();

    /// <summary>Last type of controller used to control the game.  For displaying control information.</summary>
    [Export] public ControlType ControlType
    {
        get => _controlType;
        set
        {
            _controlType = value;
            foreach ((ControlType type, CanvasItem controls) in _hints)
                controls.Visible = type == _controlType;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            ControlType = ControlType.Mouse;
            break;
        case InputEventKey:
            ControlType = ControlType.Keyboard;
            break;
        case InputEventJoypadButton:
            ControlType = ControlType.Playstation;
            break;
        case InputEventJoypadMotion when VirtualMouse.GetAnalogVector() != Vector2.Zero:
            ControlType = ControlType.Playstation;
            break;
        }
    }

    public override void _Ready()
    {
        _hints = new()
        {
            { ControlType.Mouse,       GetNode<CanvasItem>("HUD/Mouse") },
            { ControlType.Keyboard,    GetNode<CanvasItem>("HUD/Keyboard") },
            { ControlType.Playstation, GetNode<CanvasItem>("HUD/Playstation") }
        };
    }
}
