using Godot;
using TbsFramework.Nodes.StateCharts.Reactions;

namespace TbsFramework.Scenes.Level.Events.Reactions;

/// <summary>State reaction to a unit choosing a target for an action.</summary>
public partial class TargetReaction : StateReaction2<Vector2I, Vector2I>
{
    /// <summary>Signals that an action's target has been chosen.</summary>
    /// <param name="source">Cell containing the unit performing the action.</param>
    /// <param name="target">Cell the action is being performed on.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Vector2I source, Vector2I target);

    public TargetReaction() : base(SignalName.StateUpdated) {}
    public new void React(Vector2I source, Vector2I target) => base.React(source, target);
}