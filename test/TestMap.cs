using Godot;

public partial class TestMap : Node2D
{
    public async void OnObjectiveCompleted()
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);
        GD.Print("Objective complete!");
        GetTree().Quit();
    }
}
