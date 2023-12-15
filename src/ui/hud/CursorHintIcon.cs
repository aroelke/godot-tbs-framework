using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

/// <summary>Hint icon for showing the controls to move the cursor for the current control scheme.</summary>
[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private Texture2D GetKeyIcon(Key key) => KeyMap is not null && KeyMap.Contains(key) ? KeyMap[key] : null;

    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;
    private TextureRect _mouseIcon = null;
    private GamepadCursorHintIcon _gamepadIcon = null;

    private TextureRect UpKeyIcon => _upKeyIcon ??= GetNode<TextureRect>("Keyboard/Up");
    private TextureRect LeftKeyIcon => _leftKeyIcon ??= GetNode<TextureRect>("Keyboard/Left");
    private TextureRect DownKeyIcon => _downKeyIcon ??= GetNode<TextureRect>("Keyboard/Down");
    private TextureRect RightKeyIcon => _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");
    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");
    private GamepadCursorHintIcon GamepadIcon => _gamepadIcon ??= GetNode<GamepadCursorHintIcon>("Gamepad");

    /// <summary>Mapping of keyboard key onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = null;

    /// <summary>Mapping of mouse action onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public MouseIconMap MouseMap = null;

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

            GamepadIcon.UpAction = UpAction;
            GamepadIcon.LeftAction = LeftAction;
            GamepadIcon.DownAction = DownAction;
            GamepadIcon.RightAction = RightAction;
        }
    }
}