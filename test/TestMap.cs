using Godot;

[SceneTree]
public partial class TestMap : Node2D
{
    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);
        GD.Print(success ? "Success!" : "Failure...");
        GetTree().Quit();
    }

    public override void _Ready()
    {
        base._Ready();
        _.CanvasLayer.ObjectiveLabel.Text = $"Success: {_.EventController.Success?.Description ?? "None"}\nFailure: {_.EventController.Failure?.Description ?? "Never"}";
    }
}