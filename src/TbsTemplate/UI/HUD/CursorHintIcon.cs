using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Nodes.Components;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.IconMaps;

namespace TbsTemplate.UI.HUD;

/// <summary>
/// Hint icon for showing the controls to move the <see cref="Cursor"/>/<see cref="Pointer"/>
/// for the current control scheme.
/// </summary>
[Icon("res://icons/ControlHint.svg"), Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private readonly NodeCache _cache = null;
    private InputDevice _selected = default;
    private Dictionary<InputDevice, Control> _icons = [];

    private TextureRect           MouseIcon    => _cache.GetNode<TextureRect>("%MouseIcon");
    private GridContainer         KeyboardIcon => _cache.GetNode<GridContainer>("%KeyboardIcon");
    private TextureRect           UpKeyIcon    => _cache.GetNode<TextureRect>("%UpKeyIcon");
    private TextureRect           LeftKeyIcon  => _cache.GetNode<TextureRect>("%LeftKeyIcon");
    private TextureRect           DownKeyIcon  => _cache.GetNode<TextureRect>("%DownKeyIcon");
    private TextureRect           RightKeyIcon => _cache.GetNode<TextureRect>("%RightKeyIcon");
    private GamepadCursorHintIcon GamepadIcon  => _cache.GetNode<GamepadCursorHintIcon>("%GamepadIcon");

    private Texture2D GetKeyIcon(Key key) => KeyMap.ContainsKey(key) ? KeyMap[key] : null;

    private void SelectDevice(InputDevice device)
    {
        foreach ((InputDevice d, Control i) in _icons)
            i.Visible = d == device;   
    }

    private void UpdateIcons()
    {
        UpKeyIcon.Texture    = GetKeyIcon(InputManager.GetInputKeycode(InputManager.DigitalMoveUp));
        LeftKeyIcon.Texture  = GetKeyIcon(InputManager.GetInputKeycode(InputManager.DigitalMoveLeft));
        DownKeyIcon.Texture  = GetKeyIcon(InputManager.GetInputKeycode(InputManager.DigitalMoveDown));
        RightKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(InputManager.DigitalMoveRight));
        MouseIcon.Texture = MouseMap.Motion;

        GamepadIcon.ButtonMap = ButtonMap;
        GamepadIcon.AxisMap = AxisMap;
    }

    /// <summary>Mapping of <see cref="MouseButton"/> onto icon to display.</summary>
    [Export] public MouseIconMap MouseMap = null;

    /// <summary>Mapping of <see cref="Key"/> onto icon to display.</summary>
    [Export] public KeyIconMap KeyMap = null;

    /// <summary>Mapping of <see cref="JoyButton"/> onto icon to display.</summary>
    [Export] public CompositeGamepadButtonIconMap ButtonMap = null;

    /// <summary>Mapping of <see cref="JoyAxis"/> onto icon to display.</summary>
    [Export] public CompositeGamepadAxisIconMap AxisMap = null;

    /// <summary>Set the selected input device to show the icon for.</summary>
    public InputDevice SelectedDevice
    {
        get => _selected;
        set => SelectDevice(_selected = value);
    }

    public CursorHintIcon() : base() { _cache = new(this); }

    /// <summary>When the input changes, switch the icon to the correct device.</summary>
    /// <param name="device">New input device.</param>
    public void OnInputDeviceChanged(InputDevice device, string name) => SelectedDevice = device;

    public override void _EnterTree()
    {
        base._EnterTree();

        if (Engine.IsEditorHint())
            SelectDevice(SelectedDevice);
        else
        {
            UpdateIcons();
            SelectedDevice = DeviceManager.Device;
            DeviceManager.Singleton.InputDeviceChanged += OnInputDeviceChanged;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            _icons = new()
            {
                { InputDevice.Mouse,    MouseIcon    },
                { InputDevice.Keyboard, KeyboardIcon },
                { InputDevice.Gamepad,  GamepadIcon  }
            };
            SelectedDevice = DeviceManager.Device;
            SelectDevice(SelectedDevice);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
            UpdateIcons();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (!Engine.IsEditorHint())
            DeviceManager.Singleton.InputDeviceChanged -= OnInputDeviceChanged;
    }
}