using Godot;

namespace Object.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to react to input events.</summary>
public partial class InputReaction : Reaction
{
    /// <summary>Signals that the <see cref="State"/> has received an input event while active.</summary>
    /// <param name="event">Input event description.</param>
    [Signal] public delegate void StateInputEventHandler(InputEvent @event);

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (Active)
            EmitSignal(SignalName.StateInput, @event);
    }
}