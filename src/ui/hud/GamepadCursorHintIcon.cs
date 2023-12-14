using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

[Tool]
public partial class GamepadCursorHintIcon : HBoxContainer
{
    private Texture2D GetButtonIcon(JoyButton b) => ButtonMap is not null && ButtonMap.Contains(b) ? ButtonMap[b] : null;

    private TextureRect _upIcon = null, _leftIcon = null, _downIcon = null, _rightIcon = null;
    private TextureRect _unified = null;

    private Container Invdividual;
    private TextureRect UpIcon => _upIcon ??= GetNode<TextureRect>("Individual/Up");
    private TextureRect LeftIcon => _leftIcon ??= GetNode<TextureRect>("Individual/Left");
    private TextureRect DownIcon => _downIcon ??= GetNode<TextureRect>("Individual/Down");
    private TextureRect RightIcon => _rightIcon ??= GetNode<TextureRect>("Individual/Right");
    private TextureRect UnifiedIcon => _unified ??= GetNode<TextureRect>("Unified");

    [Export] public GamepadButtonIconMap ButtonMap = null;

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
            if (UpIcon is not null)
                UpIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(UpAction));
            if (LeftIcon is not null)
                LeftIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(LeftAction));
            if (DownIcon is not null)
                DownIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(DownAction));
            if (RightIcon is not null)
                RightIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(RightAction));
            
            if (UnifiedIcon is not null)
                UnifiedIcon.Texture = ButtonMap?[ButtonMap.Dpad];
        }
    }
}