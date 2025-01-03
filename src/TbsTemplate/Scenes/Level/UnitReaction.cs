using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level;

/// <summary>State reaction to an event involving a <see cref="Unit"/>.</summary>
public partial class UnitReaction : BaseReaction, IReaction<Unit>
{
    /// <summary>Signals that the <see cref="Unit"/> event has occurred.</summary>
    /// <param name="unit">Unit that caused the event to happen.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Unit unit);

    public void React(Unit value) => EmitSignal(SignalName.StateUpdated, value);

    public void OnUpdated(Unit value)
    {
        if (Active)
            React(value);
    }
}