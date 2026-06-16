using Godot;
using TbsFramework.Nodes.StateCharts.Reactions;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Events.Reactions;

/// <summary>State reaction to a unit command.</summary>
public partial class CommandReaction : StateReaction2<Vector2I, UnitAction>
{
    /// <summary>Signals that a unit has been given a command.</summary>
    /// <param name="cell">Cell containing the unit being commanded.</param>
    /// <param name="command">Command being given.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Vector2I cell, UnitAction command);

    public CommandReaction() : base(SignalName.StateUpdated) {}
    public new void React(Vector2I cell, UnitAction command) => base.React(cell, command);
}