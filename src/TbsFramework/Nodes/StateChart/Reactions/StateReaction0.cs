using Godot;

namespace TbsFramework.Nodes.StateCharts.Reactions;

/// <summary>Reaction that has no parameters.</summary>
/// <param name="signal">Name of the signal to emit when reacting.</param>
public abstract partial class StateReaction0(StringName signal) : StateReaction
{
    /// <summary>React to an event.</summary>
    public void React()
    {
        if (Active)
            EmitSignal(signal);
    }
}