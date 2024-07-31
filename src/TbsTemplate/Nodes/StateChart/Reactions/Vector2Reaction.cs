using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>State reaction to an event involving a <see cref="Vector2"/>.</summary>
public partial class Vector2Reaction : Reaction
{
    /// <summary>Signals that the <see cref="Vector2"/> event occurred.</summary>
    /// <param name="value">Value of the vector that caused the event.</param>
    [Signal] public delegate void UpdatedEventHandler(Vector2 value);

    public void OnUpdated(Vector2 value)
    {
        if (Active)
            EmitSignal(SignalName.Updated, value);
    }
}
