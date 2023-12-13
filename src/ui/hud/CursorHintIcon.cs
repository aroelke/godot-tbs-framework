using Godot;
using ui.input.map;

namespace ui.hud;

[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private Key _upKey = Key.None, _leftKey = Key.None, _downKey = Key.None, _rightKey = Key.None;
    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;

    private TextureRect UpKeyIcon => _upKeyIcon = GetNode<TextureRect>("Keyboard/Up");
    private TextureRect LeftKeyIcon => _leftKeyIcon = GetNode<TextureRect>("Keyboard/Left");
    private TextureRect DownKeyIcon => _downKeyIcon = GetNode<TextureRect>("Keyboard/Down");
    private TextureRect RightKeyIcon => _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");

    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = null;

    [ExportGroup("Keyboard Actions")]
    [Export] public Key UpKey
    {
        get => _upKey;
        set
        {
            if (KeyMap is not null && KeyMap.Contains(value))
                UpKeyIcon.Texture = KeyMap[value];
            _upKey = value;
        }
    }

    [ExportGroup("Keyboard Actions")]
    [Export] public Key LeftKey
    {
        get => _leftKey;
        set
        {
            if (KeyMap is not null && KeyMap.Contains(value))
                LeftKeyIcon.Texture = KeyMap[value];
            _leftKey = value;
        }
    }

    [ExportGroup("Keyboard Actions")]
    [Export] public Key DownKey
    {
        get => _downKey;
        set
        {
            if (KeyMap is not null && KeyMap.Contains(value))
                DownKeyIcon.Texture = KeyMap[value];
            _downKey = value;
        }
    }

    [ExportGroup("Keyboard Actions")]
    [Export] public Key RightKey
    {
        get => _rightKey;
        set
        {
            if (KeyMap is not null && KeyMap.Contains(value))
                RightKeyIcon.Texture = KeyMap[value];
            _rightKey = value;
        }
    }
}