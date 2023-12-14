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
            if (_upKeyIcon is not null)
                _upKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(UpAction));
            if (_leftKeyIcon is not null)
                _leftKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(LeftAction));
            if (_downKeyIcon is not null)
                _downKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(DownAction));
            if (_rightKeyIcon is not null)
                _rightKeyIcon.Texture = GetKeyIcon(InputManager.GetInputKeycode(RightAction));

            if (_mouseIcon is not null)
                _mouseIcon.Texture = MouseMap?[MouseMap.Motion];
        }
    }

    public override void _Ready()
    {
        base._Ready();

        _upKeyIcon = GetNode<TextureRect>("Keyboard/Up");
        _leftKeyIcon = GetNode<TextureRect>("Keyboard/Left");
        _downKeyIcon = GetNode<TextureRect>("Keyboard/Down");
        _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");
        _mouseIcon = GetNode<TextureRect>("Mouse");
    }
}