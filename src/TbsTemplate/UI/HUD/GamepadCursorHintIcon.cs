using Godot;
using TbsTemplate.Nodes.Components;
using TbsTemplate.UI.Controls.Action;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.IconMaps;

namespace TbsTemplate.UI.HUD;

/// <summary>
/// Hint icon for showing the controls to move the <see cref="Level.Object.Cursor"/>/<see cref="Level.UI.Pointer"/> for a game pad.
/// Switches between showing four buttons in a diamond pattern and showing a single directional pad depending on if all the actions
/// are mapped to the pad in the right way.
/// </summary>
[Icon("res://icons/UIIcon.svg"), Tool]
public partial class GamepadCursorHintIcon : HBoxContainer
{
    private readonly NodeCache _cache = null;

    private GridContainer IndividualIcons => _cache.GetNode<GridContainer>("%IndividualIcons");
    private TextureRect   UpIcon          => _cache.GetNode<TextureRect>("%UpIcon");
    private TextureRect   LeftIcon        => _cache.GetNode<TextureRect>("%LeftIcon");
    private TextureRect   RightIcon       => _cache.GetNode<TextureRect>("%RightIcon");
    private TextureRect   DownIcon        => _cache.GetNode<TextureRect>("%DownIcon");
    private TextureRect   UnifiedIcon     => _cache.GetNode<TextureRect>("%UnifiedIcon");
    private TextureRect   AnalogIcon      => _cache.GetNode<TextureRect>("%AnalogIcon");

    private Texture2D GetButtonIcon(JoyButton b) => ButtonMap is not null && ButtonMap.ContainsKey(b) ? ButtonMap[b] : null;

    private void Update()
    {
        UpIcon.Texture    = GetButtonIcon(InputManager.GetInputGamepadButton(InputManager.DigitalMoveUp));
        LeftIcon.Texture  = GetButtonIcon(InputManager.GetInputGamepadButton(InputManager.DigitalMoveLeft));
        DownIcon.Texture  = GetButtonIcon(InputManager.GetInputGamepadButton(InputManager.DigitalMoveDown));
        RightIcon.Texture = GetButtonIcon(InputManager.GetInputGamepadButton(InputManager.DigitalMoveRight));

        UnifiedIcon.Texture = ButtonMap.Dpad;

        AnalogIcon.Texture = InputManager.GetInputGamepadAxis(InputManager.AnalogMoveUp) switch
        {
            JoyAxis.LeftX  | JoyAxis.LeftY  => AxisMap?.LeftAxis,
            JoyAxis.RightX | JoyAxis.RightY => AxisMap?.RightAxis,
            _ => null
        };
    }

    /// <summary>Mapping of <see cref="JoyButton"/> on to icon to display.</summary>
    [Export] public CompositeGamepadButtonIconMap ButtonMap = new();

    /// <summary>Mapping of <see cref="JoyAxis"/> onto icon to display.</summary>
    [Export] public CompositeGamepadAxisIconMap AxisMap = new();

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

    public GamepadCursorHintIcon() : base() { _cache = new(this); }

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
            ShowIndividualIcons = !(InputManager.GetInputGamepadButton(InputManager.DigitalMoveUp)    == JoyButton.DpadUp   &&
                                    InputManager.GetInputGamepadButton(InputManager.DigitalMoveLeft)  == JoyButton.DpadLeft &&
                                    InputManager.GetInputGamepadButton(InputManager.DigitalMoveDown)  == JoyButton.DpadDown &&
                                    InputManager.GetInputGamepadButton(InputManager.DigitalMoveRight) == JoyButton.DpadRight);
        }
    }
}