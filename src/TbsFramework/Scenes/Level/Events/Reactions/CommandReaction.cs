using Godot;
using TbsFramework.Nodes.StateCharts.Reactions;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Events.Reactions;

/// <summary>State reaction to a unit command.</summary>
public partial class CommandReaction : StateReaction2<Unit, StringName>
{
    /// <summary>Signals that a unit has been given a command.</summary>
    /// <param name="unit">Unit being commanded.</param>
    /// <param name="command">Command being given.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Unit unit, StringName command);

    public CommandReaction() : base(SignalName.StateUpdated) {}
    public new void React(Unit unit, StringName command) => base.React(unit, command);
}