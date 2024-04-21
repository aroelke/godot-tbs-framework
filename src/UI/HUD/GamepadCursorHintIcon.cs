using Godot;
using UI.Controls.Icons;
using UI.Controls.Action;

namespace UI.HUD;

/// <summary>
/// Hint icon for showing the controls to move the <see cref="Level.Object.Cursor"/>/<see cref="Level.UI.Pointer"/> for a game pad.
/// Switches between showing four buttons in a diamond pattern and showing a single directional pad depending on if all the actions
/// are mapped to the pad in the right way.
/// </summary>
[Icon("res://icons/UIIcon.svg"), Tool]
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
        UpIcon.Texture    = GetButtonIcon(UpAction.GamepadButton);
        LeftIcon.Texture  = GetButtonIcon(LeftAction.GamepadButton);
        DownIcon.Texture  = GetButtonIcon(DownAction.GamepadButton);
        RightIcon.Texture = GetButtonIcon(RightAction.GamepadButton);

        UnifiedIcon.Texture = ButtonMap.Dpad;

        AnalogIcon.Texture = AnalogAction.GamepadAxis switch
        {
            JoyAxis.LeftX  | JoyAxis.LeftY  => AxisMap?.Left,
            JoyAxis.RightX | JoyAxis.RightY => AxisMap?.Right,
            _ => null
        };
    }

    /// <summary>Mapping of <see cref="JoyButton"/> on to icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadButtonIconMap ButtonMap = new();

    /// <summary>Mapping of <see cref="JoyAxis"/> onto icon to display.</summary>
    [ExportGroup("Icon Maps")]
    [Export] public GamepadAxisIconMap AxisMap = new();

    /// <summary>Name of the action to move the cursor up.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference UpAction = new();

    /// <summary>Name of the action to move the cursor left.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference LeftAction = new();

    /// <summary>Name of the action to move the cursor down.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference DownAction = new();

    /// <summary>Name of the action to move the cursor right.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference RightAction = new();

    /// <summary>Name of an action to move the cursor with the analog stick.</summary>
    [ExportGroup("Actions")]
    [Export] public InputActionReference AnalogAction = new();

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
            ShowIndividualIcons = !(UpAction.GamepadButton    == JoyButton.DpadUp   &&
                                    LeftAction.GamepadButton  == JoyButton.DpadLeft &&
                                    DownAction.GamepadButton  == JoyButton.DpadDown &&
                                    RightAction.GamepadButton == JoyButton.DpadRight);
        }
    }
}