using Godot;

namespace Nodes.StateChart.Reactions;

public partial class ProcessReaction : Reaction
{
    [Signal] public delegate void StateProcessEventHandler(double delta);

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Active)
            EmitSignal(SignalName.StateProcess, delta);
    }
}