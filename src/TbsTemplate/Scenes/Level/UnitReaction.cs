using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level;

/// <summary>State reaction to an event involving a <see cref="Unit"/>.</summary>
public partial class UnitReaction : Reaction1<UnitRenderer>
{
    /// <summary>Signals that the <see cref="Unit"/> event has occurred.</summary>
    /// <param name="unit">Unit that caused the event to happen.</param>
    [Signal] public delegate void StateUpdatedEventHandler(UnitRenderer unit);

    public UnitReaction() : base(SignalName.StateUpdated) {}
    public new void React(UnitRenderer value) => base.React(value);
}