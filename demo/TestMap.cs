using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI;

public partial class TestMap : Node2D
{
    public Label TurnLabel => GetNode<Label>("CanvasLayer/TurnLabel");

    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;

    /// <summary>Update the UI turn counter for the current turn and change its color to match the army.</summary>
    private void OnTurnBegan(int turn, Army army)
    {
        TurnLabel.AddThemeColorOverride("font_color", army.Faction.Color);
        TurnLabel.Text = $"Turn {turn}: {army.Faction.Name}";
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