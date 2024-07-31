using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to perform a task every process cycle.</summary>
public partial class ProcessReaction : Reaction
{
    /// <summary>Signals that a process cycle has executed while the <see cref="State"/> is active.</summary>
    /// <param name="delta">Time since the last process step (whether or not the <see cref="State"/> was active).</param>
    [Signal] public delegate void StateProcessEventHandler(double delta);

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Active)
            EmitSignal(SignalName.StateProcess, delta);
    }
}