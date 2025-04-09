using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>State reaction to a unit choosing a target for an action.</summary>
public partial class TargetReaction : Reaction2<UnitRenderer, UnitRenderer>
{
    /// <summary>Signals that an action's target has been chosen.</summary>
    /// <param name="source">Unit performing the action.</param>
    /// <param name="target">Unit the action is being performed on.</param>
    [Signal] public delegate void StateUpdatedEventHandler(UnitRenderer source, UnitRenderer target);

    public TargetReaction() : base(SignalName.StateUpdated) {}
    public new void React(UnitRenderer source, UnitRenderer target) => base.React(source, target);
}