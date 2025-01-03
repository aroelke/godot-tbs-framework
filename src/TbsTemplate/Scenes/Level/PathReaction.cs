using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;

namespace TbsTemplate.Scenes.Level;

/// <summary>State reaction to an event involving an array of <see cref="Vector2I"/>s.</summary>
public partial class PathReaction : Action1Reaction<Godot.Collections.Array<Vector2I>>
{
    /// <summary>Signals that the path event has occurred.</summary>
    /// <param name="path">Sequence of <see cref="Vector2I"/>s that caused the event.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Godot.Collections.Array<Vector2I> path);

    public PathReaction() : base(SignalName.StateUpdated) {}
    public new void React(Godot.Collections.Array<Vector2I> value) => base.React(value);
}