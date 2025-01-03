using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;

namespace TbsTemplate.Scenes.Level;

/// <summary>State reaction to an event involving an array of <see cref="Vector2I"/>s.</summary>
public partial class PathReaction : Reaction, IReaction<Godot.Collections.Array<Vector2I>>
{
    /// <summary>Signals that the path event has occurred.</summary>
    /// <param name="path">Sequence of <see cref="Vector2I"/>s that caused the event.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Godot.Collections.Array<Vector2I> path);

    public void React(Godot.Collections.Array<Vector2I> value) => EmitSignal(SignalName.StateUpdated, value);

    public void OnUpdated(Godot.Collections.Array<Vector2I> value)
    {
        if (Active)
            React(value);
    }
}