using System.Collections.Generic;
using System.Data;
using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

/// <summary>Hint icon for showing the controls to move the cursor for the current control scheme.</summary>
[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private InputManager _inputManager = null;
    private TextureRect _mouseIcon = null;
    private GridContainer _keyboardIcon = null;
    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;
    private GamepadCursorHintIcon _playstationIcon = null;

    private Dictionary<InputDevice, Control> _icons = new();

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");
    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");
    private GridContainer KeyboardIcon => _keyboardIcon ??= GetNode<GridContainer>("Keyboard");
    private TextureRect UpKeyIcon => _upKeyIcon ??= GetNode<TextureRect>("Keyboard/Up");
    private TextureRect LeftKeyIcon => _leftKeyIcon ??= GetNode<TextureRect>("Keyboard/Left");
    private TextureRect DownKeyIcon => _downKeyIcon ??= GetNode<TextureRect>("Keyboard/Down");
    private TextureRect RightKeyIcon => _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");
    private GamepadCursorHintIcon PlaystationIcon => _playstationIcon ??= GetNode<GamepadCursorHintIcon>("Playstation");

    private Texture2D GetKeyIcon(Key key) => KeyMap is not null && KeyMap.Contains(key) ? KeyMap[key] : null;

    /// <summary>Mapping of mouse action onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public MouseIconMap MouseMap = null;

    /// <summary>Mapping of keyboard key onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = null;

    [ExportGroup("Icon Maps")]
    [Export] public GamepadButtonIconMap GamepadMap = null;

    /// <summary>Name of the action for moving the cursor up.</summary>
    [ExportGroup("Actions")]
    [Export] public string UpAction = null;

    /// <summary>Name of the action for moving the cursor left.</summary>
    [ExportGroup("Actions")]
    [Export] public string LeftAction = null;

    /// <summary>Name of the action for moving the cursor down.</summary>
    [ExportGroup("Actions")]
    [Export] public string DownAction = null;

    /// <summary>Name of the action for moving the cursor right.</summary>
    [ExportGroup("Actions")]
    [Export] public string RightAction = null;

    /// <summary>Name of an action to move the cursor with the analog stick.</summary>
    [ExportGroup("Actions")]
    [Export] public string AnalogAction = null;

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
            UpKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(UpAction));
            LeftKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(LeftAction));
            DownKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(DownAction));
            RightKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(RightAction));

            MouseIcon.Texture = MouseMap?[MouseMap.Motion];

            PlaystationIcon.UpAction = UpAction;
            PlaystationIcon.LeftAction = LeftAction;
            PlaystationIcon.DownAction = DownAction;
            PlaystationIcon.RightAction = RightAction;
            PlaystationIcon.AnalogAction = AnalogAction;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        InputManager.InputDeviceChanged -= OnInputDeviceChanged;
    }
}