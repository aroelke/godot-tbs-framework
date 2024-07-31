using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to react to unhandled input events.</summary>
public partial class UnhandledInputReaction : Reaction
{
    /// <summary>Signals that an unhandled input event ocurred while the <see cref="State"/> was active.</summary>
    /// <param name="event">Input event description</param>
    [Signal] public delegate void StateUnhandledInputEventHandler(InputEvent @event);

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (Active)
            EmitSignal(SignalName.StateUnhandledInput, @event);
    }
}