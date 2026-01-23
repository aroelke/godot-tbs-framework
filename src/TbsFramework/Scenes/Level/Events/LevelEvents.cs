using System;
using Godot;
using TbsFramework.Data;
using TbsFramework.Extensions;
using TbsFramework.Nodes;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Events;

/// <summary>"Event bus" for a level that allows objects to subscribe to various events that occur during play.</summary>
public partial class LevelEvents : Node
{
    /// <summary>Auto-loaded instance of <see cref="LevelEvents"/> in case instances methods are required.</summary>
    public static LevelEvents Singleton => AutoloadNodes.GetNode<LevelEvents>("LevelEvents");
#region Level Manager
    /// <summary>Event handler for turn phases for a faction.</summary>
    /// <param name="turn">Number of the turn that ended.</param>
    /// <param name="faction">Faction whose turn phase began.</param>
    public delegate void TurnPhaseEventHandler(int turn, Faction faction);

    /// <summary>Signals that an army's turn has begun.</summary>
    public static event TurnPhaseEventHandler TurnBegan;

    /// <summary>Signals that a unit's action has ended.</summary>
    public static event Action<UnitData> ActionEnded;

    /// <summary>Signals that an army's turn has ended.</summary>
    public static event TurnPhaseEventHandler TurnEnded;

    /// <summary>Signals that the region on the map the camera is allowed to see has been updated.</summary>
    public static event Action<Rect2I> CameraBoundsUpdated;

    /// <summary>Signal that the camera should focus on something. Use <c>null</c> to keep it in place.</summary>
    public static event Action<BoundedNode2D> CameraFocused;

    /// <summary>Signal that the camera should focus on the previous thing it was focusing on.</summary>
    public static event Action CameraFocusReverted;

    /// <summary>Signal that the turn has begun for a faction.</summary>
    public static void BeginTurn(int turn, Faction faction) { if (TurnBegan is not null) TurnBegan(turn, faction); }

    /// <summary>Signal that a unit's action has ended.</summary>
    public static void EndAction(UnitData unit) { if (ActionEnded is not null) ActionEnded(unit); }

    /// <summary>Signal that the turn has ended for a faction.</summary>
    public static void EndTurn(int turn, Faction faction) { if (TurnEnded is not null) TurnEnded(turn, faction); }

    /// <summary>Signal that the camera bounds on the map have been updated.</summary>
    public static void UpdateCameraBounds(Rect2I bounds) { if (CameraBoundsUpdated is not null) CameraBoundsUpdated(bounds); }

    /// <summary>Signal that the camera should focus on a new object.</summary>
    public static void FocusCamera(BoundedNode2D target) { if (CameraFocused is not null) CameraFocused(target); }

    /// <summary>Signal that the camera should focus on its previous target.</summary>
    public static void RevertCameraFocus() { if (CameraFocusReverted is not null) CameraFocusReverted(); }
#endregion
#region Event Controllerz
    /// <summary>Signal that an event is complete, so the level can stop waiting for it.</summary>
    public static event Action EventCompleted;

    /// <summary>Signal that the success objective has been completed.</summary>
    public static event Action SuccessObjectiveCompleted;

    /// <summary>Signal that the failure objective has been completed.</summary>
    public static event Action FailureObjectiveCompleted;

    /// <summary>Signal that an event is complete so the turn can proceed.</summary>
    public static void EventComplete() { if (EventCompleted is not null) EventCompleted(); }

    /// <summary>Signal that the level success objective is complete.</summary>
    public static void SuccessObjectiveComplete() { if (SuccessObjectiveCompleted is not null) SuccessObjectiveCompleted(); }

    /// <summary>Signal that the level failure objective is complete.</summary>
    public static void FailureObjectiveComplete() { if (FailureObjectiveCompleted is not null) FailureObjectiveCompleted(); }
#endregion
#region Units
    /// <summary>Signals that a unit has been defeated.</summary>
    [Signal] public delegate void UnitDefeatedEventHandler(Unit defeated);
#endregion
}