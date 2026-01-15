using Godot;
using TbsFramework.Nodes.StateCharts.Reactions;

namespace TbsFramework.Scenes.Level.Events.Reactions;

/// <summary>State reaction to an event involving an array of <see cref="Vector2I"/>s.</summary>
public partial class PathReaction : StateReaction2<Vector2I, Godot.Collections.Array<Vector2I>>
{
    /// <summary>Signals that the path event has occurred.</summary>
    /// <param name="cell">Cell containing the unit to move along the path.</param>
    /// <param name="path">Sequence of <see cref="Vector2I"/>s that caused the event.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Vector2I cell, Godot.Collections.Array<Vector2I> path);

    public PathReaction() : base(SignalName.StateUpdated) {}
    public new void React(Vector2I cell, Godot.Collections.Array<Vector2I> value) => base.React(cell, value);
}