using Godot;

namespace TbsTemplate.Nodes.StateCharts.Reactions;

/// <summary>State reaction to an event involving a <see cref="Vector2I"/>l</summary>
public partial class Vector2IReaction : StateReaction1<Vector2I>
{
	/// <summary>Signals that a <see cref="Vector2I"/> event has occurred.</summary>
    /// <param name="value">Value of the vector that caused the reaction.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Vector2I value);

    public Vector2IReaction() : base(SignalName.StateUpdated) {}
    public new void React(Vector2I value) => base.React(value);
}