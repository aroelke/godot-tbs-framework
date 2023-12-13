using System.Collections.Generic;
using System.Linq;
using Godot;
using ui.input.map;

namespace ui.hud;

[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private string _upAction = "";
    private Key _upKey = Key.None, _leftKey = Key.None, _downKey = Key.None, _rightKey = Key.None;
    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;

    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = null;

    [ExportGroup("Keyboard Actions")]
    [Export] public string UpAction
    {
        get => _upAction;
        set
        {
            _upAction = value;
            if (Engine.IsEditorHint())
            {
                if (ProjectSettings.HasSetting($"input/{value}"))
                {
                    Godot.Collections.Array<InputEvent> events = ProjectSettings.GetSetting($"input/{value}").As<Godot.Collections.Dictionary>()["events"].As<Godot.Collections.Array<InputEvent>>();
                    List<InputEventKey> keys = events.Select((e) => e as InputEventKey).Where((e) => e is not null).ToList();
                    UpKey = keys[0].PhysicalKeycode;
                }
            }
            else
            {

            }
        }
    }

    public Key UpKey
    {
        get => _upKey;
        set
        {
            if (_upKeyIcon is not null)
            {
                GD.Print("have up key icon");
                if (KeyMap is not null && KeyMap.Contains(value))
                    _upKeyIcon.Texture = KeyMap[value];
                else
                    _upKeyIcon.Texture = default;
            }
            _upKey = value;
        }
    }

    [ExportGroup("Keyboard Actions")]
    [Export] public Key LeftKey
    {
        get => _leftKey;
        set
        {
            if (_leftKeyIcon is not null)
            {
                if (KeyMap is not null && KeyMap.Contains(value))
                    _leftKeyIcon.Texture = KeyMap[value];
                else
                    _leftKeyIcon.Texture = default;
            }
            _leftKey = value;
        }
    }

    [ExportGroup("Keyboard Actions")]
    [Export] public Key DownKey
    {
        get => _downKey;
        set
        {
            if (_downKeyIcon is not null)
            {
                if (KeyMap is not null && KeyMap.Contains(value))
                    _downKeyIcon.Texture = KeyMap[value];
                else
                    _downKeyIcon.Texture = default;
            }
            _downKey = value;
        }
    }

    [ExportGroup("Keyboard Actions")]
    [Export] public Key RightKey
    {
        get => _rightKey;
        set
        {
            if (_rightKeyIcon is not null)
            {
                if (KeyMap is not null && KeyMap.Contains(value))
                    _rightKeyIcon.Texture = KeyMap[value];
                else
                    _rightKeyIcon.Texture = default;
            }
            _rightKey = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();
        _upKeyIcon = GetNode<TextureRect>("Keyboard/Up");
        _leftKeyIcon = GetNode<TextureRect>("Keyboard/Left");
        _downKeyIcon = GetNode<TextureRect>("Keyboard/Down");
        _rightKeyIcon = GetNode<TextureRect>("Keyboard/Right");
    }
}