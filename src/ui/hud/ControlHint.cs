using System.Collections.Generic;
using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

[Tool]
public partial class ControlHint : HBoxContainer
{
    private Dictionary<InputDevice, TextureRect> _icons = new();
    private InputManager _inputManager = null;
    private TextureRect _mouseIcon = null, _keyIcon = null, _playstationIcon = null;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");
    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");
    private TextureRect KeyboardIcon => _keyIcon ??= GetNode<TextureRect>("Keyboard");
    private TextureRect PlaystationIcon => _playstationIcon ??= GetNode<TextureRect>("Playstation");

    [Export] public string InputAction { get; private set; } = "";

    [ExportGroup("Action Maps")]
    [Export] public MouseIconMap MouseMap = null;

    [ExportGroup("Action Maps")]
    [Export] public KeyIconMap KeyMap = null;

    [ExportGroup("Action Maps")]
    [Export] public GamepadButtonIconMap PlaystationMap = null;

    public InputDevice SelectedDevice
    {
        set
        {
            foreach ((InputDevice device, TextureRect icon) in _icons)
                icon.Visible = device == value;
        }
    }

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
            SelectedDevice = InputManager.Device;
            InputManager.InputDeviceChanged += OnInputDeviceChanged;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
        {
            MouseButton mb = InputManager.GetInputMouseButton(InputAction);
            MouseIcon.Texture = MouseMap is null || !MouseMap.Contains(mb) ? null : MouseMap[mb];

            Key k = InputManager.GetInputKeycode(InputAction);
            KeyboardIcon.Texture = KeyMap is null || !KeyMap.Contains(k) ? null : KeyMap[k];

            JoyButton pb = InputManager.GetInputGamepadButton(InputAction);
            PlaystationIcon.Texture = PlaystationMap is null || !PlaystationMap.Contains(pb) ? null : PlaystationMap[pb];

            GetNode<Label>("Label").Text = $": {InputAction}";
        }
    }
}