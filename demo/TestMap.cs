using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes;
using TbsTemplate.Scenes.Level;
using TbsTemplate.UI;

public partial class TestMap : Node2D
{
    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;

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
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.SuccessObjectiveComplete, () => OnObjectiveCompleted(true));
            LevelEvents.Singleton.Connect(LevelEvents.SignalName.FailureObjectiveComplete, () => OnObjectiveCompleted(false));
        }
    }
}