using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;
using TbsTemplate.Scenes.Level.Object;

/// <summary>State reaction to a unit choosing a target for an action.</summary>
public partial class TargetReaction : Reaction2<Unit, Unit>
{
    /// <summary>Signals that an action's target has been chosen.</summary>
    /// <param name="source">Unit performing the action.</param>
    /// <param name="target">Unit the action is being performed on.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Unit source, Unit target);

    public TargetReaction() : base(SignalName.StateUpdated) {}
    public new void React(Unit source, Unit target) => base.React(source, target);
}