using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>State reaction to an event involving a <see cref="Vector2I"/>l</summary>
public partial class Vector2IReaction : Reaction
{
	/// <summary>Signals that a <see cref="Vector2I"/> event has occurred.</summary>
    /// <param name="value">Value of the vector that caused the reaction.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Vector2I value);

    public void OnUpdated(Vector2I value)
    {
        if (Active)
            EmitSignal(SignalName.StateUpdated, value);
    }
}