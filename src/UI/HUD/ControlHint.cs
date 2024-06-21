using System.Collections.Generic;
using Godot;
using UI.Controls.Icons;
using UI.Controls.Action;
using UI.Controls.Device;
using Nodes;

namespace UI.HUD;

/// <summary>Icon and label showing the input for an action that doesn't have an analog or mouse option.</summary>
[Icon("res://icons/UIIcon.svg"), Tool]
public partial class ControlHint : HBoxContainer
{
    private readonly NodeCache _cache;
    public ControlHint() : base() => _cache = new(this);

    private InputActionReference _action = new();
    private InputDevice _selected = default;
    private readonly Dictionary<InputDevice, IconMap> _maps = new()
    {
        { InputDevice.Mouse,    new MouseIconMap()         },
        { InputDevice.Keyboard, new KeyIconMap()           },
        { InputDevice.Gamepad,  new GamepadButtonIconMap() }
    };

    private TextureRect Icon => _cache.GetNodeOrNull<TextureRect>("Icon");
    private Label Label => _cache.GetNodeOrNull<Label>("Label");

    private void Update(InputDevice device, InputActionReference action)
    {
        if (Icon is not null)
            Icon.Texture = _maps[device][action] ?? _maps[FallBackDevice][action];
        if (Label is not null)
            Label.Text = $": {action.Name}";
    }

    /// <summary>Action to display the icon of.</summary>
    [Export] public InputActionReference Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                _action = value;
                Update(SelectedDevice, _action);
            }
        }
    }

    /// <summary>Whether or not to fall back to the keyboard icon when a mouse icon for an action doesn't exist.</summary>
    [Export] public InputDevice FallBackDevice = InputDevice.Keyboard;

    /// <summary>Switch the device to use for the icon to display.</summary>
    [Export] public InputDevice SelectedDevice
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                Update(_selected, Action);
            }
        }
    }

    /// <summary><see cref="MouseButton"/> map for the mouse input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public MouseIconMap MouseMap
    {
        get => _maps[InputDevice.Mouse] as MouseIconMap;
        set
        {
            _maps[InputDevice.Mouse] = value;
            Update(SelectedDevice, Action);
        }
    }

    /// <summary><see cref="Key"/> map for the keyboard input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public KeyIconMap KeyMap
    {
        get => _maps[InputDevice.Keyboard] as KeyIconMap;
        set
        {
            _maps[InputDevice.Keyboard] = value;
            Update(SelectedDevice, Action);
        }
    }

    /// <summary><see cref="JoyButton"/> map for the game pad input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public GamepadButtonIconMap GamepadButtonIconMap
    {
        get => _maps[InputDevice.Gamepad] as GamepadButtonIconMap;
        set
        {
            _maps[InputDevice.Gamepad] = value;
            Update(SelectedDevice, Action);
        }
    }

    /// <summary>When the input device changes, also update the icon.</summary>
    /// <param name="device">New device being used for input.</param>
    public void OnInputDeviceChanged(InputDevice device, string name) => SelectedDevice = device;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint())
            SelectedDevice = InputDevice.Mouse;
        else
        {
            SelectedDevice = DeviceManager.Device;
            DeviceManager.Singleton.InputDeviceChanged += OnInputDeviceChanged;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged -= OnInputDeviceChanged;
    }
}