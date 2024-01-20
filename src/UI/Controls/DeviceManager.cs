using Godot;

namespace UI.Controls;

/// <summary>Manages information about and changes in input device.</summary>
public partial class DeviceManager : Node
{
    /// <summary>Signals that the input device has changed.</summary>
    /// <param name="device">New input device.</param>
    [Signal] public delegate void InputDeviceChangedEventHandler(InputDevice device, string name);

    /// <summary>Signals that the input mode has changed.</summary>
    /// <param name="mode">New input mode</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    private static InputDevice _device = InputDevice.Keyboard;
    private static string _name = "";
    private static InputMode _mode = InputMode.Digital;
    private static DeviceManager _singleton = null;

    /// <summary>Ensures that <c>InputDeviceChanged</c> is only fired once when either the device and/or device name change.</summary>
    /// <param name="device">New input device.</param>
    /// <param name="name">New input device name.</param>
    private static void UpdateDevice(InputDevice device, string name)
    {
        InputDevice oldDevice = _device;
        string oldName = _name;

        _device = device;
        _name = name;

        if (_device != oldDevice || _name != oldName)
            Singleton.EmitSignal(SignalName.InputDeviceChanged, Variant.From(_device), _name);
    }

    /// <summary>Reference to the autoloaded <c>DeviceManager</c> node so its signals can be connected.</summary>
    public static DeviceManager Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DeviceManager>("DeviceManager");

    /// <summary>The current input device.</summary>
    public static InputDevice Device
    {
        get => _device;
        set => UpdateDevice(value, _name);
    }

    /// <summary>The name of the current input device if it's a gamepad, or the empty string if it's not.</summary>
    public static string DeviceName
    {
        get => _name;
        set => UpdateDevice(_device, _name);
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
            UpdateDevice(InputDevice.Mouse, "");
            Mode = InputMode.Mouse;
            break;
        case InputEventKey:
            UpdateDevice(InputDevice.Keyboard, "");
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadButton:
            UpdateDevice(InputDevice.Gamepad, Input.GetJoyName(0));
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadMotion when InputManager.GetAnalogVector() != Vector2.Zero:
            UpdateDevice(InputDevice.Gamepad, Input.GetJoyName(0));
            Mode = InputMode.Analog;
            break;
        }
    }
}