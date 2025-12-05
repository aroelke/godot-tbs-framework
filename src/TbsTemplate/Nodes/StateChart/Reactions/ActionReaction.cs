using Godot;

namespace TbsTemplate.Nodes.StateCharts.Reactions;

/// <summary>Reaction to an action performed by another object.</summary>
public partial class ActionReaction : StateReaction0
{
    /// <summary>Signals that the connected object has raised its signal.</summary>
    [Signal] public delegate void StateUpdatedEventHandler();

    public ActionReaction() : base(SignalName.StateUpdated) {}
}