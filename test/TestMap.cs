using Godot;
using TbsTemplate.Scenes;
using TbsTemplate.Scenes.Level;
using TbsTemplate.UI;

[SceneTree]
public partial class TestMap : Node2D
{
    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;

    public void OnSuccess() => OnObjectiveCompleted(true);
    public void OnFailure() => OnObjectiveCompleted(false);

    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);

        EventController.SuccessObjectiveComplete -= OnSuccess;
        EventController.FailureObjectiveComplete -= OnFailure;
        SceneManager.Singleton.Connect(SceneManager.SignalName.SceneLoaded, Callable.From<TestGameOver>((s) => {
            MusicController.Stop();
            s.win = success;
            QueueFree();
        }), (uint)ConnectFlags.OneShot);
        SceneManager.JumpToScene(GameOverScreen);
    }

    public override void _Ready()
    {
        base._Ready();
        _.CanvasLayer.ObjectiveLabel.Text = $"Success: {_.EventController.Success?.Description ?? "None"}\nFailure: {_.EventController.Failure?.Description ?? "Never"}";

        if (!Engine.IsEditorHint())
        {
            EventController.SuccessObjectiveComplete += OnSuccess;
            EventController.FailureObjectiveComplete += OnFailure;
        }
    }
}