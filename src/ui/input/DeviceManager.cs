using Godot;

namespace ui.input;

/// <summary>Manages information about and changes in input device.</summary>
public partial class DeviceManager : Node
{
    /// <summary>Signals that the input device has changed.</summary>
    /// <param name="device">New input device.</param>
    [Signal] public delegate void InputDeviceChangedEventHandler(InputDevice device);

    /// <summary>Signals that the input mode has changed.</summary>
    /// <param name="mode">New input mode</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    private static InputDevice _device = InputDevice.Keyboard;
    private static InputMode _mode = InputMode.Digital;
    private static DeviceManager _singleton = null;

    /// <summary>Reference to the autoloaded <c>DeviceManager</c> node so its signals can be connected.</summary>
    public static DeviceManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DeviceManager>("DeviceManager");

    /// <summary>The current input device.</summary>
    public static InputDevice Device
    {
        get => _device;
        set
        {
            InputDevice old = _device;
            _device = value;
            if (_device != old)
                Singleton.EmitSignal(SignalName.InputDeviceChanged, Variant.From(_device));
        }
    }

    /// <summary>The current input mode.</summary>
    public static InputMode Mode
    {
        get => _mode;
        set
        {
            InputMode old = _mode;
            _mode = value;
            if (_mode != old)
                Singleton.EmitSignal(SignalName.InputModeChanged, Variant.From(_mode));
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
        case InputEventJoypadMotion when InputManager.GetAnalogVector() != Vector2.Zero:
            Device = InputDevice.Playstation;
            Mode = InputMode.Analog;
            break;
        }
    }
}