#pragma warning disable IDE1006 // Naming Styles

using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Objectives;

namespace TbsFramework.Scenes.Level.Events;

/// <summary>
/// Object controlling scriptable aspects of a level.  Interacts with the <see cref="LevelManager"/> to advance the state of the level and handles
/// objectives. Each event has an overridable method like <see cref="Node"/> does, which starts with an underscore for consistency with <see cref="Node"/>'s
/// ones.
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
                LevelEvents.SuccessObjectiveComplete();
            return true;
        }
        else if (Failure?.Complete ?? false)
        {
            if (signal)
                LevelEvents.FailureObjectiveComplete();
            return true;
        }
        else
            return false;
    }

    /// <summary>Skip the event.  Just evaluate the objectives, and, if none are accomplished, continue the turn.</summary>
    public void SkipEvent()
    {
        if (!EvaluateObjective())
            LevelEvents.EventComplete();
    }

    /// <summary>
    /// Event to perform before an army's turn begins. By default, evaluates the objectives and signals to start the turn if not success or failure.
    /// Overriding classes should signal <c>EventComplete</c> when they're ready for the turn to begin.
    /// </summary>
    /// <param name="turn">Turn number that's about to begin.</param>
    /// <param name="faction">Faction that's about to begin its turn.</param>
    public virtual void _TurnBegan (int turn, Faction faction) => SkipEvent();
    public         void OnTurnBegan(int turn, Faction Faction) => Callable.From<int, Faction>(_TurnBegan).CallDeferred(turn, Faction);

    /// <summary>
    /// Event to perform just after a unit ends its action. By default, evaluates the objectives and signals to continue to the next action if not success
    /// or failure. Overriding classes should signal <c>EventComplete</c> when they're ready for the turn to end.
    /// </summary>
    /// <param name="unit">Unit that just acted.</param>
    public virtual void _ActionEnded (Unit unit) => SkipEvent();
    public         void OnActionEnded(Unit unit) => Callable.From<Unit>(_ActionEnded).CallDeferred(unit);

    public virtual void _TurnEnded (int turn, Faction faction) => SkipEvent();
    public         void OnTurnEnded(int turn, Faction faction) => Callable.From<int, Faction>(_TurnEnded).CallDeferred(turn, faction);

    public override void _EnterTree()
    {
        base._EnterTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.TurnBegan += OnTurnBegan;
            LevelEvents.ActionEnded += OnActionEnded;
            LevelEvents.TurnEnded += OnTurnEnded;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.TurnBegan -= OnTurnBegan;
            LevelEvents.ActionEnded -= OnActionEnded;
            LevelEvents.TurnEnded -= OnTurnEnded;
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles