using Godot;
using ui.input;
using ui.input.map;

namespace ui.hud;

/// <summary>
/// Hint icon for showing the controls to move the cursor for a particular game pad.  Switches between showing four buttons in
/// a diamond pattern and showing a single directional pad depending on if all the actions are mapped to the pad in the right way.
/// </summary>
[Tool]
public partial class GamepadCursorHintIcon : HBoxContainer
{
    private Texture2D GetButtonIcon(JoyButton b) => ButtonMap is not null && ButtonMap.Contains(b) ? ButtonMap[b] : null;

    private GridContainer _individual = null;
    private TextureRect _upIcon = null, _leftIcon = null, _downIcon = null, _rightIcon = null;
    private TextureRect _unified = null;

    private GridContainer Individual => _individual ??= GetNode<GridContainer>("Individual");
    private TextureRect UpIcon => _upIcon ??= GetNode<TextureRect>("Individual/Up");
    private TextureRect LeftIcon => _leftIcon ??= GetNode<TextureRect>("Individual/Left");
    private TextureRect DownIcon => _downIcon ??= GetNode<TextureRect>("Individual/Down");
    private TextureRect RightIcon => _rightIcon ??= GetNode<TextureRect>("Individual/Right");
    private TextureRect UnifiedIcon => _unified ??= GetNode<TextureRect>("Unified");

    /// <summary>Mapping of game pad button on to icon to display.</summary>
    [Export] public GamepadButtonIconMap ButtonMap = null;

    /// <summary>Whether to show the individual control icons or the unified one.</summary>
    public bool ShowIndividual
    {
        get => Individual.Visible;
        set
        {
            Individual.Visible = value;
            UnifiedIcon.Visible = !value;
        }
    }

    /// <summary>Name of the action to move the cursor up.</summary>
    [ExportGroup("Actions")]
    [Export] public string UpAction = "";

    /// <summary>Name of the action to move the cursor left.</summary>
    [ExportGroup("Actions")]
    [Export] public string LeftAction = "";

    /// <summary>Name of the action to move the cursor down.</summary>
    [ExportGroup("Actions")]
    [Export] public string DownAction = "";

    /// <summary>Name of the action to move the cursor right.</summary>
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
        else
        {
            ShowIndividual = !(InputManager.GetInputGamepadButton(UpAction)    == JoyButton.DpadUp &&
                               InputManager.GetInputGamepadButton(LeftAction)  == JoyButton.DpadLeft &&
                               InputManager.GetInputGamepadButton(DownAction)  == JoyButton.DpadDown &&
                               InputManager.GetInputGamepadButton(RightAction) == JoyButton.DpadRight);
        }
    }
}