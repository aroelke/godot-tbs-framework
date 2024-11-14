using Godot;

public partial class TestMap : Node2D
{
    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);
        GD.Print(success ? "Success!" : "Failure...");
        GetTree().Quit();
    }
}
