using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private Texture2D GetKeyIcon(Key key) => KeyMap is not null && KeyMap.Contains(key) ? KeyMap[key] : null;

    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;
    private TextureRect _mouseIcon = null;

    private TextureRect UpKeyIcon => _upKeyIcon ??= GetNode<TextureRect>("Keyboard/Up");
    private TextureRect LeftKeyIcon => _leftKeyIcon ??= GetNode<TextureRect>("Keyboard/Left");
    private TextureRect DownKeyIcon => _downKeyIcon ??= GetNode<TextureRect>("Keyboard/Down");
    private TextureRect RightKeyIcon => _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");
    private TextureRect MouseIcon => _mouseIcon ??= GetNode<TextureRect>("Mouse");

    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = null;

    [ExportGroup("Icon Maps")]
    [Export] public MouseIconMap MouseMap = null;

    [ExportGroup("Actions")]
    [Export] public string UpAction = "";

    [ExportGroup("Actions")]
    [Export] public string LeftAction = "";

    [ExportGroup("Actions")]
    [Export] public string DownAction = "";

    [ExportGroup("Actions")]
    [Export] public string RightAction = "";

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
        {
            if (UpKeyIcon is not null)
                UpKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(UpAction));
            if (LeftKeyIcon is not null)
                LeftKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(LeftAction));
            if (DownKeyIcon is not null)
                DownKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(DownAction));
            if (RightKeyIcon is not null)
                RightKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(RightAction));

            if (MouseIcon is not null)
                MouseIcon.Texture = MouseMap?[MouseMap.Motion];
        }
    }
}