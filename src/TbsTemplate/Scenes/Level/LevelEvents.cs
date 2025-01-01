using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level;

/// <summary>"Event bus" for a level that allows objects to subscribe to various events that occur during play.</summary>
public partial class LevelEvents : Node
{
    /// <summary>Auto-loaded instance of <see cref="LevelEvents"/> in case instances methods are required.</summary>
    public static LevelEvents Singleton => AutoloadNodes.GetNode<LevelEvents>("LevelEvents");
#region Level Manager
    /// <summary>Signals that an army's turn has begun.</summary>
    /// <param name="turn">Number of the turn that began.</param>
    /// <param name="army">Army whose turn began.</param>
    [Signal] public delegate void TurnBeganEventHandler(int turn, Army army);

    /// <summary>Signals that a unit's action has ended.</summary>
    [Signal] public delegate void ActionEndedEventHandler(Unit unit);

    [Signal] public delegate void TurnEndedEventHandler(int turn, Army army);
#endregion
#region Event Controller
    /// <summary>Signal that an event is complete, so the level can stop waiting for it.</summary>
    [Signal] public delegate void EventCompleteEventHandler();

    /// <summary>Signal that the success objective has been completed.</summary>
    [Signal] public delegate void SuccessObjectiveCompleteEventHandler();

    /// <summary>Signal that the failure objective has been completed.</summary>
    [Signal] public delegate void FailureObjectiveCompleteEventHandler();
#endregion
#region Units
    /// <summary>Signals that a unit has been defeated.</summary>
    [Signal] public delegate void UnitDefeatedEventHandler(Unit defeated);

    /// <summary>Signals that a <see cref="Unit"/> has been chosen to act.</summary>
    /// <param name="unit">Selected unit.</param>
    [Signal] public delegate void UnitSelectedEventHandler(Unit unit);

    /// <summary>Signals that new cells have been added to a unit's potential movement path.</summary>
    /// <param name="unit">Unit that will be moving.</param>
    /// <param name="path">Path including the new cells.</param>
    [Signal] public delegate void PathUpdatedEventHandler(Unit unit, Godot.Collections.Array<Vector2I> path);

    /// <summary>Signals that a path for a unit to move on has been chosen.</summary>
    /// <param name="unit">Unit to move.</param>
    /// <param name="path">Contiguous list of cells for the unit to move through.</param>
    [Signal] public delegate void UnitMovedEventHandler(Unit unit, Godot.Collections.Array<Vector2I> path);

    /// <summary>Signals that an action has been chosen for a unit.</summary>
    /// <param name="unit">Unit performing the action.</param>
    /// <param name="command">String representing the action to perform.</param>
    [Signal] public delegate void UnitCommandedEventHandler(Unit unit, StringName command);

    /// <summary>Signals that a target for an action has been chosen.</summary>
    /// <param name="source">Unit performing the action.</param>
    /// <param name="target">Target of the action.</param>
    [Signal] public delegate void TargetChosenEventHandler(Unit source, Unit target);
#endregion
}