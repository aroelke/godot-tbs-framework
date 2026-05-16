#pragma warning disable IDE1006 // Naming Styles

using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Objectives;

namespace TbsFramework.Scenes.Level.Events;

/// <summary>
/// Object controlling scriptable aspects of a level.  Interacts with the <see cref="LevelManager"/> to advance the state of the level and handles
/// objectives. Each event has an overridable method like <see cref="Node"/> does, which starts with an underscore for consistency with <see cref="Node"/>'s
/// ones. Make sure to signal that the event is complete by calling <see cref="LevelEvents.EventComplete"/> to continue with the turn. Note also that
/// the event methods are called using <see cref="Callable.CallDeferred(Variant[])"/> so that any other objects listening for the event can perform
/// their actions before the event controller signals the end of the event.
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

    /// <summary>Event to perform before an army's turn begins. By default, evaluates the objectives and signals to start the turn if not success or failure.</summary>
    /// <param name="turn">Turn number that's about to begin.</param>
    /// <param name="faction">Faction that's about to begin its turn.</param>
    public virtual void _TurnBegan (int turn, Faction faction) => SkipEvent();
    public         void OnTurnBegan(int turn, Faction Faction) => Callable.From(() => _TurnBegan(turn, Faction)).CallDeferred();

    /// <summary>
    /// Event to perform just after a unit ends its action. By default, evaluates the objectives and signals to continue to the next action if not success
    /// or failure.
    /// </summary>
    /// <param name="unit">Unit that just acted.</param>
    public virtual void _ActionEnded (UnitData unit) => SkipEvent();
    public         void OnActionEnded(UnitData unit) => Callable.From(() => _ActionEnded(unit)).CallDeferred();

    /// <summary>Event to perform before an army's turn ends. By default, evaluates the objectives and signals to end the turn if not success or failure.</summary>
    /// <param name="turn">Turn number that's about to begin.</param>
    /// <param name="faction">Faction that's about to begin its turn.</param>
    public virtual void _TurnEnded (int turn, Faction faction) => SkipEvent();
    public         void OnTurnEnded(int turn, Faction faction) => Callable.From(() => _TurnEnded(turn, faction)).CallDeferred();

    /// <summary>Event to perform before a round ends. By default, evaluates the objectives and signals to end the round if not success or failure.</summary>
    /// <param name="round">Round number that's about to end.</param>
    public virtual void _RoundEnded (int round) => SkipEvent();
    public         void OnRoundEnded(int round) => Callable.From(() => _RoundEnded(round)).CallDeferred();

    public override void _EnterTree()
    {
        base._EnterTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.TurnBegan += OnTurnBegan;
            LevelEvents.ActionEnded += OnActionEnded;
            LevelEvents.TurnEnded += OnTurnEnded;
            LevelEvents.RoundEnded += OnRoundEnded;
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
            LevelEvents.RoundEnded -= OnRoundEnded;
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles