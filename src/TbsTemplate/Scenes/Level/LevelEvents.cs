using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level;

/// <summary>"Event bus" for a level that allows objects to subscribe to various events that occur during play.</summary>
public partial class LevelEvents : Node
{
    /// <summary>
    /// Auto-loaded instance of <see cref="LevelEvents"/> in case instances methods are required. Signal connection methods are statically
    /// overriden.
    /// </summary>
    public static LevelEvents Singleton => AutoloadNodes.GetNode<LevelEvents>("LevelEvents");
#region Level Manager
    /// <summary>Signals that an army's turn has begun.</summary>
    /// <param name="turn">Number of the turn that began.</param>
    /// <param name="army">Army whose turn began.</param>
    [Signal] public delegate void TurnBeganEventHandler(int turn, Army army);

    /// <summary>Signals that a unit's action has ended.</summary>
    [Signal] public delegate void ActionEndedEventHandler(Unit unit);
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
#endregion
}