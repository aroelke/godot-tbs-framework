using Godot;
using UI.Controls.Icons;
using UI.Controls.Action;
using Nodes;
using UI.Controls.Device;

namespace UI.HUD;

/// <summary>
/// Hint icon for showing the controls to move the <see cref="Level.Object.Cursor"/>/<see cref="Level.UI.Pointer"/> for a game pad.
/// Switches between showing four buttons in a diamond pattern and showing a single directional pad depending on if all the actions
/// are mapped to the pad in the right way.
/// </summary>
[Icon("res://icons/UIIcon.svg"), SceneTree, Tool]
public partial class GamepadCursorHintIcon : HBoxContainer
{
    private Texture2D GetButtonIcon(JoyButton b) => ButtonMap is not null && ButtonMap.ContainsKey(b) ? ButtonMap[b] : null;

    private GridContainer _individual = null;
    private TextureRect _upIcon = null, _leftIcon = null, _downIcon = null, _rightIcon = null;
    private TextureRect _unified = null;
    private TextureRect _analog = null;

    private void Update()
    {
        UpIcon.Texture    = GetButtonIcon(InputManager.GetInputGamepadButton(InputActions.DigitalMoveUp));
        LeftIcon.Texture  = GetButtonIcon(InputManager.GetInputGamepadButton(InputActions.DigitalMoveLeft));
        DownIcon.Texture  = GetButtonIcon(InputManager.GetInputGamepadButton(InputActions.DigitalMoveDown));
        RightIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(InputActions.DigitalMoveRight));

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
            ShowIndividualIcons = !(InputManager.GetInputGamepadButton(InputActions.DigitalMoveUp)    == JoyButton.DpadUp   &&
                                    InputManager.GetInputGamepadButton(InputActions.DigitalMoveLeft)  == JoyButton.DpadLeft &&
                                    InputManager.GetInputGamepadButton(InputActions.DigitalMoveDown)  == JoyButton.DpadDown &&
                                    InputManager.GetInputGamepadButton(InputActions.DigitalMoveRight) == JoyButton.DpadRight);
        }
    }
}