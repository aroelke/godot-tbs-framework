using Godot;

namespace ui.input;

public partial class InputManager : Node2D
{
    /// <summary>Signals that the input method has changed.</summary>
    /// <param name="device">New method of input.</param>
    [Signal] public delegate void InputDeviceChangedEventHandler(InputDevice device);

    /// <summary>Signals that the input mode has changed.</summary>
    /// <param name="mode">New input mode</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    /// <summary>Signals that the mouse has entered the screen.</summary>
    /// <param name="position">Position the mouse entered the screen on (depending on the mouse speed, it might not be on the edge).</param>
    [Signal] public delegate void MouseEnteredEventHandler(Vector2 position);

    /// <summary>Signals that the mouse has exited the screen.</summary>
    /// <param name="position">Position on the edge of the screen the mouse exited.</param>
    [Signal] public delegate void MouseExitedEventHandler(Vector2 position);

    /// <returns>A vector representing the digital direction(s) being held down. Elements have values 0, 1, or -1.</returns>
    public static Vector2I GetDigitalVector() => (Vector2I)Input.GetVector("cursor_digital_left", "cursor_digital_right", "cursor_digital_up", "cursor_digital_down").Round();

    /// <returns>A vector representing the movement of the left control stick of the game pad.</returns>
    public static Vector2 GetAnalogVector() => Input.GetVector("cursor_analog_left", "cursor_analog_right", "cursor_analog_up", "cursor_analog_down");

    private InputDevice _device = InputDevice.Keyboard;
    private InputMode _mode = InputMode.Digital;

    /// <summary>The current input device.</summary>
    [Export] public InputDevice Device
    {
        get => _device;
        set
        {
            InputDevice old = _device;
            _device = value;
            if (_device != old)
                EmitSignal(SignalName.InputDeviceChanged, Variant.From(_device));
        }
    }

    /// <summary>The current input mode.</summary>
    [Export] public InputMode Mode
    {
        get => _mode;
        set
        {
            InputMode old = _mode;
            _mode = value;
            if (_mode != old)
                EmitSignal(SignalName.InputModeChanged, Variant.From(_mode));
        }
    }

    /// <summary>Last known position the mouse was on the screen if it's off the screen, or <c>null</c> if it's on the screen.</summary>
    public Vector2? LastKnownPointerPosition { get; private set; } = null;

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
        case NotificationWMMouseEnter or NotificationVpMouseEnter:
            LastKnownPointerPosition = null;
            EmitSignal(SignalName.MouseEntered, GetViewport().GetMousePosition());
            break;
        case NotificationWMMouseExit or NotificationVpMouseExit:
            LastKnownPointerPosition = GetViewport().GetMousePosition().Clamp(Vector2.Zero, GetViewportRect().Size);
            EmitSignal(SignalName.MouseExited, LastKnownPointerPosition.Value);
            break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            Device = InputDevice.Mouse;
            Mode = InputMode.Mouse;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            break;
        case InputEventKey:
            Device = InputDevice.Keyboard;
            Mode = InputMode.Digital;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            break;
        case InputEventJoypadButton:
            Device = InputDevice.Playstation;
            Mode = InputMode.Digital;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            break;
        case InputEventJoypadMotion when GetAnalogVector() != Vector2.Zero:
            Device = InputDevice.Playstation;
            Mode = InputMode.Analog;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            break;
        }
    }
}