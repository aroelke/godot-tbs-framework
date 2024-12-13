using System;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.Scenes.Level.Objectives;

namespace TbsTemplate.Scenes.Level;

/// <summary>
/// Object controlling scriptable aspects of a level.  Interacts with the <see cref="LevelManager"/> to advance the state of the level and handles
/// objectives.
/// </summary>
public partial class EventController : Node
{
    /// <summary>Objective to complete for success of the level.</summary>
    [Export] public Objective Success = null;

    /// <summary>Objective to complete for failure of the level.</summary>
    [Export] public Objective Failure = null;

    /// <summary>Evaluate the success and failure objectives.</summary>
    /// <param name="signal">Whether the <c>ObjectiveCompleted</c> signal should be emitted if either objective is complete.</param>
    /// <returns><c>true</c> if either objective is complete, and <c>false</c> otherwise.</returns>
    public bool EvaluateObjective(bool signal=true)
    {
        if (Success?.Complete ?? false)
        {
            if (signal)
                LevelEvents.EmitSignal(LevelEvents.SignalName.SuccessObjectiveComplete);
            return true;
        }
        else if (Failure?.Complete ?? false)
        {
            if (signal)
                LevelEvents.EmitSignal(LevelEvents.SignalName.FailureObjectiveComplete);
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Event to perform before an army's turn begins. By default, evaluates the objectives and signals to start the turn if not success or failure.
    /// Overriding classes should signal <see cref="EventComplete"/> when they're ready for the turn to begin.
    /// </summary>
    /// <param name="turn">Turn number that's about to begin.</param>
    /// <param name="army">Army that's about to begin its turn.</param>
    public virtual void OnTurnBegan(int turn, Army army)
    {
        if (!EvaluateObjective())
            LevelEvents.EmitSignal(LevelEvents.SignalName.EventComplete);
    }

    /// <summary>
    /// Event to perform just after a unit ends its action. By default, evaluates the objectives and signals to continue to the next action if not success
    /// or failure. Overriding classes should signal <see cref="EventComplete"/> when they're ready for the turn to end.
    /// </summary>
    /// <param name="unit">Unit that just acted.</param>
    public virtual void OnActionEnded(Unit unit)
    {
        if (!EvaluateObjective())
            LevelEvents.EmitSignal(LevelEvents.SignalName.EventComplete);
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            LevelEvents.Connect(LevelEvents.SignalName.TurnBegan, Callable.From<int, Army>(OnTurnBegan));
            LevelEvents.Connect(LevelEvents.SignalName.ActionEnded, Callable.From<Unit>(OnActionEnded));
        }
    }
}