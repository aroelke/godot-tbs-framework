using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Events.Reactions;

/// <summary>State reaction to an event involving an array of <see cref="Vector2I"/>s.</summary>
public partial class PathReaction : Reaction2<Unit, Godot.Collections.Array<Vector2I>>
{
    /// <summary>Signals that the path event has occurred.</summary>
    /// <param name="unit">Unit to move along the path.</param>
    /// <param name="path">Sequence of <see cref="Vector2I"/>s that caused the event.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Unit unit, Godot.Collections.Array<Vector2I> path);

    public PathReaction() : base(SignalName.StateUpdated) {}
    public new void React(Unit unit, Godot.Collections.Array<Vector2I> value) => base.React(unit, value);
}