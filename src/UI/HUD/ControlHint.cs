using System.Collections.Generic;
using System.Linq;
using Godot;
using ui.Action;
using ui.Device.Icons;
using ui.Device;

namespace ui.HUD;

/// <summary>Icon and label showing the input for an action that doesn't have an analog or mouse option.</summary>
[Tool]
public partial class ControlHint : HBoxContainer
{
    private Dictionary<InputDevice, TextureRect> _icons = new();
    private TextureRect _mouseIcon = null, _keyIcon = null, _gamepadIcon = null;

    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");
    private TextureRect KeyboardIcon => _keyIcon ??= GetNode<TextureRect>("Keyboard");
    private TextureRect GamepadIcon => _gamepadIcon ??= GetNode<TextureRect>("Gamepad");

    private void Update()
    {
        MouseIcon.Texture = !MouseMap.ContainsKey(Action.MouseButton) ? null : MouseMap[Action.MouseButton];
        KeyboardIcon.Texture = !KeyMap.ContainsKey(Action.Key) ? null : KeyMap[Action.Key];
        GamepadIcon.Texture = !GamepadMap.ContainsKey(Action.GamepadButton) ? null : GamepadMap[Action.GamepadButton];

        GetNode<Label>("Label").Text = $": {Action.InputAction.ToString().Split(".").Last()}";
    }

    /// <summary>Action to display the icon of.</summary>
    [Export] public InputActionReference Action = new();

    /// <summary>Mouse button map for the mouse input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public MouseIconMap MouseMap = new();

    /// <summary>Keyboard map for the keyboard input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public KeyIconMap KeyMap = new();

    /// <summary>Button map for the Playstation game pad input to the action.</summary>
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

    /// <summary>When the input mode changes, also update the icon.</summary>
    /// <param name="device">New device being used for input.</param>
    public void OnInputDeviceChanged(InputDevice device, string name) => SelectedDevice = device;

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
            DeviceManager.Singleton.InputDeviceChanged += OnInputDeviceChanged;

            Update();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
            Update();
    }
}