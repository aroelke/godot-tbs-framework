using Godot;

namespace ui;

/// <summary>
/// Method used for moving the virtual mouse cursor: the real mouse, using digital input (e.g. keyboard keys, controller dpad), or
/// using analog inputs (e.g. controller analog sticks).
/// </summary>
public enum InputMode
{
    Mouse,
    Digital,
    Analog
}

/// <summary>Types of input controller supported.</summary>
public enum InputController
{
    Mouse,
    Keyboard,
    Playstation
}

/// <summary>Watcher for changes in control input.</summary>
public partial class Controller : Node
{
    /// <summary>Signals that the input controller has changed.</summary>
    /// <param name="type">New type of input controller.</param>
    [Signal] public delegate void ControllerChangedEventHandler(InputController type);

    private InputController _controlType = InputController.Mouse;

    /// <summary>Last type of controller used to control the game.  For displaying control information.</summary>
    [Export] public InputController InputController
    {
        get => _controlType;
        set => EmitSignal(SignalName.ControllerChanged, Variant.From(_controlType = value));
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            InputController = InputController.Mouse;
            break;
        case InputEventKey:
            InputController = InputController.Keyboard;
            break;
        case InputEventJoypadButton:
            InputController = InputController.Playstation;
            break;
        case InputEventJoypadMotion when VirtualMouse.GetAnalogVector() != Vector2.Zero:
            InputController = InputController.Playstation;
            break;
        }
    }
}
