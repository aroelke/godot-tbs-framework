using Godot;
using TbsTemplate.Nodes.StateChart.Reactions;
using TbsTemplate.Scenes.Level.Object;

/// <summary>State reaction to a unit command.</summary>
public partial class CommandReaction : Reaction2<Unit, StringName>
{
    /// <summary>Signals that a unit has been given a command.</summary>
    /// <param name="unit">Unit being commanded.</param>
    /// <param name="command">Command being given.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Unit unit, StringName command);

    public CommandReaction() : base(SignalName.StateUpdated) {}
    public new void React(Unit unit, StringName command) => base.React(unit, command);
}