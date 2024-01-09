using System.Collections.Generic;
using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

/// <summary>Icon and label showing the input for an action that doesn't have an analog or mouse option.</summary>
[Tool]
public partial class ControlHint : HBoxContainer
{
    private Dictionary<InputDevice, TextureRect> _icons = new();
    private TextureRect _mouseIcon = null, _keyIcon = null, _playstationIcon = null;

    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");
    private TextureRect KeyboardIcon => _keyIcon ??= GetNode<TextureRect>("Keyboard");
    private TextureRect PlaystationIcon => _playstationIcon ??= GetNode<TextureRect>("Playstation");

    private void Update()
    {
        MouseButton mb = InputManager.GetInputMouseButton(InputAction);
        MouseIcon.Texture = MouseMap is null || !MouseMap.ContainsKey(mb) ? null : MouseMap[mb];

        Key k = InputManager.GetInputKeycode(InputAction);
        KeyboardIcon.Texture = KeyMap is null || !KeyMap.ContainsKey(k) ? null : KeyMap[k];

        JoyButton pb = InputManager.GetInputGamepadButton(InputAction);
        PlaystationIcon.Texture = PlaystationMap is null || !PlaystationMap.ContainsKey(pb) ? null : PlaystationMap[pb];

        GetNode<Label>("Label").Text = $": {InputAction}";
    }

    /// <summary>Input action shown by the hint.</summary>
    [Export] public string InputAction { get; private set; } = "";

    /// <summary>Mouse button map for the mouse input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public MouseIconMap MouseMap = null;

    /// <summary>Keyboard map for the keyboard input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public KeyIconMap KeyMap = null;

    /// <summary>Button map for the Playstation game pad input to the action.</summary>
    [ExportGroup("Action Maps")]
    [Export] public GamepadButtonIconMap PlaystationMap = null;

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
    public void OnInputDeviceChanged(InputDevice device) => SelectedDevice = device;

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            _icons = new()
            {
                { InputDevice.Mouse, MouseIcon },
                { InputDevice.Keyboard, KeyboardIcon },
                { InputDevice.Playstation, PlaystationIcon }
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