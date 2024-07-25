using System.Collections.Generic;
using Godot;
using UI.Controls.Icons;
using UI.Controls.Action;
using UI.Controls.Device;

namespace UI.HUD;

/// <summary>
/// Hint icon for showing the controls to move the <see cref="Level.Object.Cursor"/>/<see cref="Level.UI.Pointer"/>
/// for the current control scheme.
/// </summary>
[Icon("res://icons/UIIcon.svg"), SceneTree, Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private Dictionary<InputDevice, Control> _icons = new();

    private Texture2D GetKeyIcon(Key key) => KeyMap.ContainsKey(key) ? KeyMap[key] : null;

    /// <summary>Mapping of <see cref="MouseButton"/> onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public MouseIconMap MouseMap = new();

    /// <summary>Mapping of <see cref="Key"/> onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = new();

    /// <summary>Mapping of <see cref="JoyButton"/> onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadButtonIconMap ButtonMap = new();

    /// <summary>Mapping of <see cref="JoyAxis"/> onto icon to display.</summary>
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
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged -= OnInputDeviceChanged;
    }
}