using Godot;

namespace TbsTemplate.Nodes.StateCharts.Reactions;

/// <summary>Reaction that has no parameters.</summary>
/// <param name="signal">Name of the signal to emit when reacting.</param>
public abstract partial class Reaction0(StringName signal) : Reaction
{
    /// <summary>React to an event.</summary>
    public void React()
    {
        if (Active)
            EmitSignal(signal);
    }
}