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

    private Dictionary<InputDevice, TextureRect> _icons = new();

    private TextureRect MouseIcon => _cache.GetNode<TextureRect>("Mouse");
    private TextureRect KeyboardIcon => _cache.GetNode<TextureRect>("Keyboard");
    private TextureRect GamepadIcon => _cache.GetNode<TextureRect>("Gamepad");

    private void Update()
    {
        MouseIcon.Texture = !MouseMap.ContainsKey(Action.MouseButton) ? null : MouseMap[Action.MouseButton];
        KeyboardIcon.Texture = !KeyMap.ContainsKey(Action.Key) ? null : KeyMap[Action.Key];
        GamepadIcon.Texture = !GamepadMap.ContainsKey(Action.GamepadButton) ? null : GamepadMap[Action.GamepadButton];

        _cache.GetNode<Label>("Label").Text = $": {Action.Name}";
    }

    /// <summary>Action to display the icon of.</summary>
    [Export] public InputActionReference Action = new();

    /// <summary><see cref="MouseButton"/> map for the mouse input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public MouseIconMap MouseMap = new();

    /// <summary><see cref="Key"/>  map for the keyboard input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public KeyIconMap KeyMap = new();

    /// <summary><see cref="JoyButton"/> map for the game pad input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public GamepadButtonIconMap GamepadMap = new();

    /// <summary>Switch the device to use for the icon to display.</summary>
    public InputDevice SelectedDevice
    {
        set
        {
            foreach ((InputDevice device, TextureRect icon) in _icons)
                icon.Visible = device == value;
        }
    }

    public ControlHint() : base()
    {
        _cache = new(this);
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
        if (!Engine.IsEditorHint())
        {
            _icons = new()
            {
                { InputDevice.Mouse, MouseIcon },
                { InputDevice.Keyboard, KeyboardIcon },
                { InputDevice.Gamepad, GamepadIcon }
            };
            SelectedDevice = DeviceManager.Device;

            Update();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
            Update();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged -= OnInputDeviceChanged;
    }
}