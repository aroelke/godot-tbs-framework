using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to react to input events.</summary>
public partial class InputReaction : BaseReaction, IReaction<InputEvent>
{
    /// <summary>Signals that the <see cref="State"/> has received an input event while active.</summary>
    /// <param name="event">Input event description.</param>
    [Signal] public delegate void StateInputEventHandler(InputEvent @event);

    public void React(InputEvent value) => EmitSignal(SignalName.StateInput, value);

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (Active)
            React(@event);
    }
}