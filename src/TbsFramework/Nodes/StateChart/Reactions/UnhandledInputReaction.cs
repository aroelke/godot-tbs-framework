using Godot;

namespace TbsFramework.Nodes.StateCharts.Reactions;

/// <summary>Allows a <see cref="State"/> to react to unhandled input events.</summary>
public partial class UnhandledInputReaction : StateReaction1<InputEvent>
{
    /// <summary>Signals that an unhandled input event ocurred while the <see cref="State"/> was active.</summary>
    /// <param name="event">Input event description</param>
    [Signal] public delegate void StateUnhandledInputEventHandler(InputEvent @event);

    public UnhandledInputReaction() : base(SignalName.StateUnhandledInput) {}

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        React(@event);
    }
}