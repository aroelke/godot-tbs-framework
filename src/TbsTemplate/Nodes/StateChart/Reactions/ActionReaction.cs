using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Reaction that has no parameters.</summary>
/// <param name="signal">Name of the signal to emit when reacting.</param>
public abstract partial class ActionReaction(StringName signal) : Reaction
{
    /// <summary>React to an event.</summary>
    public void React()
    {
        if (Active)
            EmitSignal(signal);
    }
}