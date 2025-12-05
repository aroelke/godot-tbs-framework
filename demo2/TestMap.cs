using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.HUD;

namespace TbsTemplate.Demo2;

public partial class TestMap : Node2D
{
    public Label TurnLabel => GetNode<Label>("CanvasLayer/TurnLabel");
    public TextureProgressBar TurnProgress => GetNode<TextureProgressBar>("CanvasLayer/TurnProgress");

    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;

    /// <summary>Update the UI turn counter for the current turn and change its color to match the army.</summary>
    public void OnTurnBegan(int turn, Army army)
    {
        TurnLabel.AddThemeColorOverride("font_color", army.Faction.Color);
        TurnLabel.Text = $"Turn {turn}: {army.Faction.Name}";
    }

    public void OnEnabledInputActionsUpdated(StringName[] actions)
    {
        foreach (ControlHint hint in GetNode("CanvasLayer/HUD/Hints").GetChildren().OfType<ControlHint>())
            hint.Visible = actions.Contains(hint.Get(ControlHint.PropertyName.Action).AsStringName());
        GetNode<CanvasItem>("CanvasLayer/HUD/Hints/CursorHintIcon").Visible = actions.Intersect([
            InputManager.DigitalMoveUp, InputManager.DigitalMoveLeft, InputManager.DigitalMoveDown, InputManager.DigitalMoveRight,
            InputManager.AnalogMoveUp,  InputManager.AnalogMoveLeft,  InputManager.AnalogMoveDown,  InputManager.AnalogMoveRight
        ]).Any();
    }

    public void OnArmyControllerFastForwardStateChanged(bool enable) => TurnProgress.Visible = enable;

    public void OnArmyControllerProgressUpdated(int completed, int remaining)
    {
        TurnProgress.MaxValue = completed + remaining;
        TurnProgress.Value = completed;
    }

    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);

        SceneManager.Singleton.Connect<TestGameOver>(SceneManager.SignalName.SceneLoaded, (s) => {
            MusicController.Stop();
            s.win = success;
            QueueFree();
        }, (uint)ConnectFlags.OneShot);
        SceneManager.JumpToScene(GameOverScreen);
    }

    public override void _Ready()
    {
        base._Ready();
        GetNode<Label>("CanvasLayer/ObjectiveLabel").Text = $"Success: {GetNode<EventController>("EventController").Success?.Description ?? "None"}\nFailure: {GetNode<EventController>("EventController").Failure?.Description ?? "Never"}";

        if (!Engine.IsEditorHint())
        {
            LevelEvents.Singleton.Connect<int, Army>(LevelEvents.SignalName.TurnBegan, OnTurnBegan);
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.SuccessObjectiveComplete, () => OnObjectiveCompleted(true));
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.FailureObjectiveComplete, () => OnObjectiveCompleted(false));
        }
    }
}