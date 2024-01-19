using System.Collections.Generic;
using Godot;
using ui.Action;
using ui.Device.Icons;
using ui.Device;

namespace ui.HUD;

/// <summary>Hint icon for showing the controls to move the cursor for the current control scheme.</summary>
[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private TextureRect _mouseIcon = null;
    private GridContainer _keyboardIcon = null;
    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;
    private GamepadCursorHintIcon _gamepadIcon = null;

    private Dictionary<InputDevice, Control> _icons = new();

    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");
    private GridContainer KeyboardIcon => _keyboardIcon ??= GetNode<GridContainer>("Keyboard");
    private TextureRect UpKeyIcon => _upKeyIcon ??= GetNode<TextureRect>("Keyboard/Up");
    private TextureRect LeftKeyIcon => _leftKeyIcon ??= GetNode<TextureRect>("Keyboard/Left");
    private TextureRect DownKeyIcon => _downKeyIcon ??= GetNode<TextureRect>("Keyboard/Down");
    private TextureRect RightKeyIcon => _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");
    private GamepadCursorHintIcon GamepadIcon => _gamepadIcon ??= GetNode<GamepadCursorHintIcon>("Gamepad");

    private Texture2D GetKeyIcon(Key key) => KeyMap.ContainsKey(key) ? KeyMap[key] : null;

    /// <summary>Mapping of mouse action onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public MouseIconMap MouseMap = new();

    /// <summary>Mapping of keyboard key onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = new();

    /// <summary>Mapping of gamepad button onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadButtonIconMap ButtonMap = new();

    /// <summary>Mapping of gamepad axis onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadAxisIconMap AxisMap = new();

    /// <summary>Name of the action for moving the cursor up.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference UpAction = new();

    /// <summary>Name of the action for moving the cursor left.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference LeftAction = new();

    /// <summary>Name of the action for moving the cursor down.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference DownAction = new();

    /// <summary>Name of the action for moving the cursor right.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference RightAction = new();

    /// <summary>Name of an action to move the cursor with the analog stick.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference AnalogAction = new();

    /// <summary>Set the selected input device to show the icon for.</summary>
    public InputDevice SelectedDevice
    {
        set
        {
            foreach ((InputDevice device, Control icon) in _icons)
                icon.Visible = device == value;
        }
    }

    /// <summary>When the input changes, switch the icon to the correct device.</summary>
    /// <param name="device">New input device.</param>
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
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
        {
            UpKeyIcon.Texture = GetKeyIcon(UpAction.Key);
            LeftKeyIcon.Texture = GetKeyIcon(LeftAction.Key);
            DownKeyIcon.Texture = GetKeyIcon(DownAction.Key);
            RightKeyIcon.Texture = GetKeyIcon(RightAction.Key);
            MouseIcon.Texture = MouseMap.Motion;

            GamepadIcon.ButtonMap = ButtonMap;
            GamepadIcon.AxisMap = AxisMap;
            GamepadIcon.UpAction = UpAction;
            GamepadIcon.LeftAction = LeftAction;
            GamepadIcon.DownAction = DownAction;
            GamepadIcon.RightAction = RightAction;
        }
    }
}