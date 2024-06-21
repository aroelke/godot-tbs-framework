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

    private InputDevice _selected = InputDevice.Keyboard;
    private readonly Dictionary<InputDevice, IconMap> _maps = new()
    {
        { InputDevice.Mouse,    new MouseIconMap()         },
        { InputDevice.Keyboard, new KeyIconMap()           },
        { InputDevice.Gamepad,  new GamepadButtonIconMap() }
    };

    private TextureRect Icon => _cache.GetNodeOrNull<TextureRect>("Icon");
    private Label Label => _cache.GetNodeOrNull<Label>("Label");

    private void Update()
    {
        Icon.Texture = !_maps[SelectedDevice].ContainsKey(Action) ? null : _maps[SelectedDevice][Action];
        Label.Text = $": {Action.Name}";
    }

    /// <summary>Action to display the icon of.</summary>
    [Export] public InputActionReference Action = new();

    /// <summary>Whether or not to fall back to the keyboard icon when a mouse icon for an action doesn't exist.</summary>
    [Export] public bool FallBackToKeyboard = false;

    /// <summary>Switch the device to use for the icon to display.</summary>
    [Export] public InputDevice SelectedDevice
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                if (Icon is not null && Label is not null)
                    Update();
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
            if (Icon is not null && Label is not null)
                Update();
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
            if (Icon is not null && Label is not null)
                Update();
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
            if (Icon is not null && Label is not null)
                Update();
        }
    }

    /// <summary>When the input device changes, also update the icon.</summary>
    /// <param name="device">New device being used for input.</param>
    public void OnInputDeviceChanged(InputDevice device, string name) => SelectedDevice = device;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged += OnInputDeviceChanged;
    }

    public override void _Ready()
    {
        base._Ready();
        SelectedDevice = Engine.IsEditorHint() ? InputDevice.Mouse : DeviceManager.Device;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged -= OnInputDeviceChanged;
    }
}