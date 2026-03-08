using Godot;
using TbsFramework.Nodes.StateCharts.Reactions;

namespace TbsFramework.Scenes.Level.Events.Reactions;

/// <summary>State reaction to a unit command.</summary>
public partial class CommandReaction : StateReaction2<Vector2I, StringName>
{
    /// <summary>Signals that a unit has been given a command.</summary>
    /// <param name="cell">Cell containing the unit being commanded.</param>
    /// <param name="command">Command being given.</param>
    [Signal] public delegate void StateUpdatedEventHandler(Vector2I cell, StringName command);

    public CommandReaction() : base(SignalName.StateUpdated) {}
    public new void React(Vector2I cell, StringName command) => base.React(cell, command);
}