using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>State reaction to a unit command.</summary>
public partial class CommandReaction : Reaction2<UnitRenderer, StringName>
{
    /// <summary>Signals that a unit has been given a command.</summary>
    /// <param name="unit">Unit being commanded.</param>
    /// <param name="command">Command being given.</param>
    [Signal] public delegate void StateUpdatedEventHandler(UnitRenderer unit, StringName command);

    public CommandReaction() : base(SignalName.StateUpdated) {}
    public new void React(UnitRenderer unit, StringName command) => base.React(unit, command);
}