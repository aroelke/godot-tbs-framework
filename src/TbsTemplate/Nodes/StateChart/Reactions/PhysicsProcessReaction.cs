using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to perform a task every physics process cycle.</summary>
public partial class PhysicsProcessReaction : Reaction, IReaction<double>
{
    /// <summary>Signals that a physics process cycle has executed while the <see cref="State"/> is active.</summary>
    /// <param name="delta">Time since the last physics process step (whether or not the <see cref="State"/> was active).</param>
    [Signal] public delegate void StatePhysicsProcessEventHandler(double delta);

    public void React(double value) => EmitSignal(SignalName.StatePhysicsProcess, value);

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Active)
            React(delta);
    }
}