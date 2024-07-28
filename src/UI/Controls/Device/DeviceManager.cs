using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Nodes.Components;

namespace UI.Controls.Device;

/// <summary>Manages information about and changes in input device.</summary>
[Tool]
public partial class DeviceManager : Node
{
    /// <summary>Signals that the input device has changed.</summary>
    /// <param name="device">New input device.</param>
    [Signal] public delegate void InputDeviceChangedEventHandler(InputDevice device, string name);

    /// <summary>Signals that the input mode has changed.</summary>
    /// <param name="mode">New input mode</param>
    [Signal] public delegate void InputModeChangedEventHandler(InputMode mode);

    private static readonly StringName GamepadDigitalModeActivatorsProperty = "GamepadDigitalModeActivators";

    private static InputDevice _device = InputDevice.Keyboard;
    private static string _name = "";
    private static InputMode _mode = InputMode.Digital;
    private static DeviceManager _singleton = null;

    /// <summary>Ensures that <see cref="InputDeviceChanged"/> is only fired once when either the device and/or device name change.</summary>
    /// <param name="device">New input device.</param>
    /// <param name="name">New input device name.</param>
    private static void UpdateDevice(InputDevice device, string name="")
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

    private HashSet<JoyButton> _digitalSwitchButtons = [];
    private StringName[] _digitalSwitchActions = [];

    /// <summary>Dead zone to use for detecting gamepad axes.</summary>
    [Export] public float MotionDeadzone = 0.5f;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.Add(new()
        {
            { "name", GamepadDigitalModeActivatorsProperty },
            { "type", Variant.From(Variant.Type.Array) },
            { "hint", Variant.From(PropertyHint.TypeString) },
            { "hint_string", $"{(int)Variant.Type.StringName}/{(int)PropertyHint.Enum}:{IHasInputActionProperties.InputActionList}" }
        });
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (property == GamepadDigitalModeActivatorsProperty)
            return _digitalSwitchActions;
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == GamepadDigitalModeActivatorsProperty)
        {
            _digitalSwitchActions = value.As<StringName[]>();
            return true;
        }
        return base._Set(property, value);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == GamepadDigitalModeActivatorsProperty)
            return Array.Empty<StringName>();
        return base._PropertyGetRevert(property);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (property == GamepadDigitalModeActivatorsProperty)
            return _digitalSwitchActions.Length > 0;
        return base._PropertyCanRevert(property);
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            _digitalSwitchButtons = _digitalSwitchActions.Select(static (a) => {
                IEnumerable<JoyButton> buttons = InputMap.ActionGetEvents(a).OfType<InputEventJoypadButton>().Select(static (e) => e.ButtonIndex);
                if (buttons.Any())
                    return buttons.First();
                else
                    return JoyButton.Invalid;
            }).Where(static (b) => b != JoyButton.Invalid).ToHashSet();
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        switch (@event)
        {
        case InputEventMouse:
            UpdateDevice(InputDevice.Mouse);
            Mode = InputMode.Mouse;
            break;
        case InputEventKey:
            UpdateDevice(InputDevice.Keyboard);
            Mode = InputMode.Digital;
            break;
        case InputEventJoypadButton b:
            UpdateDevice(InputDevice.Gamepad, Input.GetJoyName(0));
            if (_digitalSwitchButtons.Contains(b.ButtonIndex))
                Mode = InputMode.Digital;
            break;
        case InputEventJoypadMotion e when Mathf.Abs(e.AxisValue) >= MotionDeadzone:
            UpdateDevice(InputDevice.Gamepad, Input.GetJoyName(0));
            Mode = InputMode.Analog;
            break;
        }
    }
}