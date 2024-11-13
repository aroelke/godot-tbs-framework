using Godot;

public partial class TestMap : Node2D
{
    public async void OnObjectiveUpdated(bool complete)
    {
        if (complete)
        {
            await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);
            GD.Print("Success!");
            GetTree().Quit();
        }
    }

    public async void OnObjectiveUpdated2(bool complete)
    {
        if (complete)
        {
            await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);
            GD.Print("Failure");
            GetTree().Quit();
        }
    }
}
