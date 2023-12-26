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
    private Texture2D GetButtonIcon(JoyButton b) => ButtonMap is not null && ButtonMap.ContainsKey(b) ? ButtonMap[b] : null;

    private GridContainer _individual = null;
    private TextureRect _upIcon = null, _leftIcon = null, _downIcon = null, _rightIcon = null;
    private TextureRect _unified = null;
    private TextureRect _analog = null;

    private GridContainer IndividualIcons => _individual ??= GetNode<GridContainer>("Individual");
    private TextureRect UpIcon => _upIcon ??= GetNode<TextureRect>("Individual/Up");
    private TextureRect LeftIcon => _leftIcon ??= GetNode<TextureRect>("Individual/Left");
    private TextureRect DownIcon => _downIcon ??= GetNode<TextureRect>("Individual/Down");
    private TextureRect RightIcon => _rightIcon ??= GetNode<TextureRect>("Individual/Right");
    private TextureRect UnifiedIcon => _unified ??= GetNode<TextureRect>("Unified");
    private TextureRect AnalogIcon => _analog ??= GetNode<TextureRect>("Analog");

    private void Update()
    {
        UpIcon.Texture    = GetButtonIcon(InputManager.GetInputGamepadButton(UpAction));
        LeftIcon.Texture  = GetButtonIcon(InputManager.GetInputGamepadButton(LeftAction));
        DownIcon.Texture  = GetButtonIcon(InputManager.GetInputGamepadButton(DownAction));
        RightIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(RightAction));

        UnifiedIcon.Texture = ButtonMap?.Dpad;

        AnalogIcon.Texture = InputManager.GetInputGamepadAxis(AnalogAction) switch
        {
            JoyAxis.LeftX  | JoyAxis.LeftY  => AxisMap?.Left,
            JoyAxis.RightX | JoyAxis.RightY => AxisMap?.Right,
            _ => null
        };
    }

    /// <summary>Mapping of game pad button on to icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadButtonIconMap ButtonMap = null;

    /// <summary>Mapping of game pad axis onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadAxisIconMap AxisMap = null;

    /// <summary>Name of the action to move the cursor up.</summary>
    [ExportGroup("Actions")]
    [Export] public string UpAction = null;

    /// <summary>Name of the action to move the cursor left.</summary>
    [ExportGroup("Actions")]
    [Export] public string LeftAction = null;

    /// <summary>Name of the action to move the cursor down.</summary>
    [ExportGroup("Actions")]
    [Export] public string DownAction = null;

    /// <summary>Name of the action to move the cursor right.</summary>
    [ExportGroup("Actions")]
    [Export] public string RightAction = null;

    /// <summary>Name of an action to move the cursor with the analog stick.</summary>
    [ExportGroup("Actions")]
    [Export] public string AnalogAction = null;

    /// <summary>Whether to show the individual control icons or the unified one.</summary>
    public bool ShowIndividualIcons
    {
        get => IndividualIcons.Visible;
        set
        {
            if (!Engine.IsEditorHint())
            {
                IndividualIcons.Visible = value;
                UnifiedIcon.Visible = !value;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            Update();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
            Update();
        else
        {
            ShowIndividualIcons = !(InputManager.GetInputGamepadButton(UpAction)    == JoyButton.DpadUp   &&
                                    InputManager.GetInputGamepadButton(LeftAction)  == JoyButton.DpadLeft &&
                                    InputManager.GetInputGamepadButton(DownAction)  == JoyButton.DpadDown &&
                                    InputManager.GetInputGamepadButton(RightAction) == JoyButton.DpadRight);
        }
    }
}