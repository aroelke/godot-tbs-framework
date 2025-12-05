using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.HUD;

namespace TbsTemplate.Demo;

/// <summary>Script for the demo map that controls events outside level progression, suc as UI updates.</summary>
public partial class DemoMap : Node
{
    public void OnTurnBegan(int turn, Army army)
    {
        Label label = GetNode<Label>("CanvasLayer/HUD/Status/TurnLabel");
        label.AddThemeColorOverride("font_color", army.Faction.Color);
        label.Text = $"Turn {turn}: {army.Faction.Name}";
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

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.Connect<int, Army>(LevelEvents.SignalName.TurnBegan, OnTurnBegan);
    }
}