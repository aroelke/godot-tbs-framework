using System.Collections.Generic;
using System.Linq;
using Godot;
using ui.input.map;

namespace ui.hud;

[Tool]
public partial class CursorHintIcon : HBoxContainer
{
    private static Key GetKeyCode(string name)
    {
        if (Engine.IsEditorHint())
        {
            string setting = $"input/{name}";
            if (ProjectSettings.HasSetting(setting))
            {
                Godot.Collections.Array<InputEvent> events = ProjectSettings.GetSetting(setting).As<Godot.Collections.Dictionary>()["events"].As<Godot.Collections.Array<InputEvent>>();
                List<InputEventKey> keys = events.Select((e) => e as InputEventKey).Where((e) => e is not null).ToList();
                return keys[0].PhysicalKeycode;
            }
        }
        else
        {

        }
        return Key.None;
    }

    private Texture2D GetKeyIcon(Key key) => KeyMap is not null && KeyMap.Contains(key) ? KeyMap[key] : default;

    private string _upAction = "", _leftAction = "", _downAction = "", _rightAction = "";
    private TextureRect _upKeyIcon = null, _leftKeyIcon = null, _downKeyIcon = null, _rightKeyIcon = null;
    private TextureRect _mouseIcon = null;

    [ExportGroup("Icon Maps")]
    [Export] public KeyIconMap KeyMap = null;

    [ExportGroup("Icon Maps")]
    [Export] public MouseIconMap MouseMap = null;

    [ExportGroup("Actions")]
    [Export] public string UpAction
    {
        get => _upAction;
        set
        {
            _upAction = value;
            if (_upKeyIcon is not null)
                _upKeyIcon.Texture = GetKeyIcon(GetKeyCode(_upAction));
        }
    }

    [ExportGroup("Actions")]
    [Export] public string LeftAction
    {
        get => _leftAction;
        set
        {
            _leftAction = value;
            if (_leftKeyIcon is not null)
                _leftKeyIcon.Texture = GetKeyIcon(GetKeyCode(_leftAction));
        }
    }

    [ExportGroup("Actions")]
    [Export] public string DownAction
    {
        get => _downAction;
        set
        {
            _downAction = value;
            if (_downKeyIcon is not null)
                _downKeyIcon.Texture = GetKeyIcon(GetKeyCode(_downAction));
        }
    }

    [ExportGroup("Actions")]
    [Export] public string RightAction
    {
        get => _rightAction;
        set
        {
            _rightAction = value;
            if (_rightKeyIcon is not null)
                _rightKeyIcon.Texture = GetKeyIcon(GetKeyCode(_rightAction));
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
        if (MouseMap is not null)
            _mouseIcon.Texture = MouseMap[MouseMap.Motion];
    }
}