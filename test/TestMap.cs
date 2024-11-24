using Godot;
using TbsTemplate.Scenes;

[SceneTree]
public partial class TestMap : Node2D
{
    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;

    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);

        SceneManager.Singleton.Connect(SceneManager.SignalName.SceneLoaded, Callable.From<Node>((n) => (n as TestGameOver).win = success), (uint)ConnectFlags.OneShot);
        SceneManager.ChangeScene(GameOverScreen);
    }

    public override void _Ready()
    {
        base._Ready();
        _.CanvasLayer.ObjectiveLabel.Text = $"Success: {_.EventController.Success?.Description ?? "None"}\nFailure: {_.EventController.Failure?.Description ?? "Never"}";
    }
}