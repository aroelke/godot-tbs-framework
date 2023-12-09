using Godot;

namespace ui.input;

public partial class InputManager : Node
{
    /// <summary>Signals that the input method has changed.</summary>
    /// <param name="device">New method of input.</param>
    [Signal] public delegate void InputDeviceChangedEventHandler(InputDevice device);

    /// <summary>Signals that the input mode has changed.</summary>
    /// <param name="mode">New input mode</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    /// <returns>A vector representing the movement of the left control stick of the game pad.</returns>
    public static Vector2 GetAnalogVector() => Input.GetVector("cursor_analog_left", "cursor_analog_right", "cursor_analog_up", "cursor_analog_down");

    private InputDevice _device = InputDevice.Mouse;
    private InputMode _mode = InputMode.Mouse;

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

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            Device = InputDevice.Mouse;
            Mode = InputMode.Mouse;
            break;
        case InputEventKey:
            Device = InputDevice.Keyboard;
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadButton:
            Device = InputDevice.Playstation;
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadMotion when GetAnalogVector() != Vector2.Zero:
            Device = InputDevice.Playstation;
            Mode = InputMode.Analog;
            break;
        }
    }
}