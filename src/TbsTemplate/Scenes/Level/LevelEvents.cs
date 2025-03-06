using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level;

/// <summary>"Event bus" for a level that allows objects to subscribe to various events that occur during play.</summary>
public partial class LevelEvents : Node
{
    /// <summary>Auto-loaded instance of <see cref="LevelEvents"/> in case instances methods are required.</summary>
    public static LevelEvents Singleton => AutoloadNodes.GetNode<LevelEvents>("LevelEvents");
#region Level Manager
    /// <summary>Signals that the region on the map the camera is allowed to see has been updated.</summary>
    /// <param name="bounds">Rectangle bounding the camera movement.</param>
    [Signal] public delegate void CameraBoundsUpdatedEventHandler(Rect2I bounds);

    /// <summary>Signals that an army's turn has begun.</summary>
    /// <param name="turn">Number of the turn that began.</param>
    /// <param name="army">Army whose turn began.</param>
    [Signal] public delegate void TurnBeganEventHandler(int turn, Army army);

    /// <summary>Signals that a unit's action has ended.</summary>
    [Signal] public delegate void ActionEndedEventHandler(Unit unit);

    /// <summary>Signals that an army's turn has ended.</summary>
    /// <param name="turn">Number of the turn that ended.</param>
    /// <param name="army">Army whose turn ended.</param>
    [Signal] public delegate void TurnEndedEventHandler(int turn, Army army);

    /// <summary>Signal that the camera should focus on something. Use <c>null</c> to keep it in place.</summary>
    [Signal] public delegate void FocusCameraEventHandler(BoundedNode2D target);

    /// <summary>Signal that the camera should focus on the previous thing it was focusing on.</summary>
    [Signal] public delegate void RevertCameraFocusEventHandler();
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