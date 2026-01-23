using System.Linq;
using Godot;
using TbsFramework.Data;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.UI.Controls.Device;
using TbsFramework.UI.HUD;

namespace TbsFramework.Demo;

/// <summary>Script for the demo map that controls events outside level progression, suc as UI updates.</summary>
public partial class DemoMap : Node
{
    public void OnTurnBegan(int turn, Faction faction)
    {
        Label label = GetNode<Label>("CanvasLayer/HUD/Status/TurnLabel");
        label.AddThemeColorOverride("font_color", faction.Color);
        label.Text = $"Turn {turn}: {faction.Name}";
    }

    public void OnEnabledInputActionsUpdated(StringName[] actions)
    {
        foreach (ControlHint hint in GetNode("CanvasLayer/HUD/Hints").GetChildren().OfType<ControlHint>())
            hint.Visible = actions.Contains(hint.Get(ControlHint.PropertyName.Action).AsStringName());
        GetNode<CanvasItem>("CanvasLayer/HUD/Hints/CursorHint").Visible = actions.Intersect([
            InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveDown, InputManager.DigitalMoveRight,
            InputManager.AnalogMoveUp,  InputManager.AnalogMoveLeft,  InputManager.AnalogMoveDown,  InputManager.AnalogMoveRight
        ]).Any();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
            LevelEvents.TurnBegan += OnTurnBegan;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            LevelEvents.TurnBegan -= OnTurnBegan;
    }
}