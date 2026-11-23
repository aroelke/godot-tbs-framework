using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control.Evaluation;
using TbsFramework.Scenes.Rendering;
using TbsFramework.Scenes.Transitions;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="BehaviorNode"/>s and the state of the level.</summary>
[Icon("uid://bambqlpd7p4kr"), Tool]
public partial class AIController : ArmyController
{
    private const int HealthDiffPrecision = 10;

    /// <summary>Signals that the fast-forward state has changed.</summary>
    /// <param name="enable"><c>true</c> if fast-forwarding is in progress, and <c>false</c> otherwise.</param>
    [Signal] public delegate void FastForwardStateChangedEventHandler(bool enable);

    private class VirtualAction
    {
        private Vector2I _destination = GridData.InvalidCell;

        public VirtualAction(UnitData actor, UnitAction action, Vector2I destination, Vector2I target, IEnumerable<Vector2I> traversable)
        {
            Actor = actor;
            Start = actor?.Cell ?? GridData.InvalidCell;
            Action = action;
            Traversable = traversable;
            Destination = destination;
            Target = target;
        }

        public UnitData Actor;
        public UnitAction Action;
        public Vector2I Target = GridData.InvalidCell;
        public IEnumerable<Vector2I> Traversable;
        public Vector2I Start = GridData.InvalidCell;

        public Path Path { get; private set; }

        public Vector2I Destination
        {
            get => _destination;
            set
            {
                _destination = value;
                Path = Path.Empty(Traversable, Actor.CellCost).Add(Start).Add(_destination);
            }
        }

        public GridData Result = null;
    }

    private static List<VirtualAction> GetAvailableActions(GridData grid, Faction faction, IEnumerable<UnitAction> available)
    {
        List<VirtualAction> actions = [];
        foreach ((_, UnitData unit) in grid.Occupants)
        {
            if (unit.Faction == faction && unit.Active && unit.Health > 0)
            {
                IEnumerable<Vector2I> traversable = unit.GetTraversableCells();
                foreach (PerformableAction action in unit.Behavior.GetActions(unit, available, traversable))
                {
                    IEnumerable<Vector2I> destinations;

                    destinations = action.Sources;
                    if (action.Action.RequiresTarget)
                    {
                        // If the action allows for retaliation, prioritize cells that the target can't retaliate on
                        if (action.Action.RequiresTarget && action.Action.RetaliationAllowed)
                        {
                            IEnumerable<Vector2I> safe = destinations.Where((c) => !action.Action.CanPerform(unit.Grid.Occupants[action.Target], action.Target, c));
                            if (safe.Any())
                                destinations = safe;
                        }
                    }

                    // Prioritize the destination closest to the actor's current cell, but if that cell is the actor's current cell then
                    // also try the next-best one in case moving another unit to that cell afterward is overall better. Always use
                    // Manhattan distance here rather than the behavior's destination preference for performance reasons and also because
                    // there are paths to all choices anyway, so it doesn't matter how one is chosen
                    Vector2I best = destinations.MinBy((c) => c.ManhattanDistanceTo(unit.Cell));
                    actions.Add(new(unit, action.Action, best, action.Target, traversable));
                    if (best == unit.Cell && destinations.Count() > 1)
                        actions.Add(new(unit, action.Action, destinations.Where((c) => c != best).MinBy((c) => c.ManhattanDistanceTo(unit.Cell)), action.Target, traversable));
                }
            }
        }

        return actions;
    }

    private double GetActionValue(VirtualAction action, IEnumerable<UnitAction> available, int remaining, Dictionary<GridData, double> cache)
    {
        action.Result = action.Action.Simulate(action.Actor, action.Destination, action.Target);
        action.Result.Occupants[action.Destination].Active = false;
        SimulatedAction simulated = new(action.Result, Faction, action.Path, action.Action);

        if (action.Result.Occupants[action.Destination].Health <= 0)
            action.Result.Occupants[action.Destination].Grid = null;
        if (action.Action.RequiresTarget && action.Result.Occupants[action.Target].Health <= 0)
            action.Result.Occupants[action.Target].Grid = null;

        if (!cache.TryGetValue(action.Result, out double value))
        {
            // Intentionally accumulate value over the whole tree to increase value of high-weight actions that happen earlier
            value = CurrentActionCoefficient*Evaluators.Select((e) => e.Key.Evaluate(simulated)*e.Value).DefaultIfEmpty(0).Sum();
            if (remaining > 1 || remaining == 0 && NextActionCoefficient != 0)
                value += NextActionCoefficient*GetAvailableActions(action.Result, action.Actor.Faction, available).Select((a) => GetActionValue(a, available, remaining - 1, cache)).DefaultIfEmpty(0).Max();
            cache[action.Result] = value;
        }
        return value;
    }

    private class GridDataEqualityComparer(int seed = 31333, int mod = 227) : IEqualityComparer<GridData>
    {
        public bool Equals(GridData x, GridData y)
        {
            if (x.Occupants.Count != y.Occupants.Count)
                return false;
            static int comparison(Vector2I a, Vector2I b) => a < b ? -1 : a == b ? 0 : 1;
            IEnumerable<UnitData> xOccupants = x.Occupants.Values.OrderBy((u) => u.Cell, comparison);
            IEnumerable<UnitData> yOccupants = y.Occupants.Values.OrderBy((u) => u.Cell, comparison);
            foreach ((UnitData a, UnitData b) in xOccupants.Zip(yOccupants))
                if (a.Cell != b.Cell || a.Identity != b.Identity || a.Health != b.Health)
                    return false;
            return true;
        }

        public int GetHashCode([DisallowNull] GridData obj) => obj.Occupants.Values.Select((u) => HashCode.Combine(u.Cell, u.Identity, u.Health)).Aggregate(seed, (a, b) => mod*a + b);
    }

    public (UnitData selected, Vector2I destination, UnitAction action, Vector2I target) ComputeAction(IEnumerable<UnitAction> actions)
    {
        UnitData selected;
        Vector2I destination;
        UnitAction action = null;
        Vector2I target;

        List<VirtualAction> potential = [.. GetAvailableActions(Grid.Data, Faction, actions.Where((a) => a.AIAllowed))];
        if (potential.Count != 0)
        {
            Func<double> task(VirtualAction action) => () => GetActionValue(action, actions, MaxSearchDepth, new Dictionary<GridData, double>(new GridDataEqualityComparer()));
            VirtualAction result;
            if (EvaluateWithThreads)
                result = potential.Zip(Task.WhenAll([.. potential.Select((a) => Task.Run(task(a)))]).Result).MaxBy((a) => a.Second).First;
            else
            {
                Dictionary<GridData, VirtualAction> decisions = [];
                result = potential.Zip(potential.Select((a) => task(a)())).MaxBy((a) => a.Second).First;
            }
            selected = result.Actor;
            destination = result.Destination;
            action = result.Action;
            target = result.Target;
        }
        else
        {
            IEnumerable<UnitData> enemies = Grid.Data.Occupants.Values.Where((o) => o is UnitData u && !u.Faction.AlliedTo(Faction)).OfType<UnitData>();

            IEnumerable<UnitData> available = Faction.GetUnits(Grid.Data).Where((u) => u.Active);
            selected = enemies.Any() ? available.MinBy((u) => enemies.Min((e) => u.Cell.DistanceTo(e.Cell))) : available.First();
            IEnumerable<Vector2I> destinations = selected.GetTraversableCells().Where((c) => c == selected.Cell || !Grid.Data.Occupants.ContainsKey(c));
            action = actions.FirstOrDefault((a) => a.AIAllowed && !a.RequiresTarget && destinations.Any((c) => a.CanPerform(selected, c)));
            destination = selected.Behavior.ChooseDestination(selected, destinations);
            target = GridData.InvalidCell;
        }

        return (selected, destination, action, target);
    }

    private readonly NodeCache _cache = null;
    private UnitData _selected = null;
    private Vector2I _destination = GridData.InvalidCell;
    private UnitAction _action = null;
    private UnitData _target = null;
    private bool _ff = false;

    private Sprite2D              Pseudocursor          => _cache.GetNode<Sprite2D>("Pseudocursor");
    private FadeToBlackTransition FastForwardTransition => _cache.GetNode<FadeToBlackTransition>("CanvasLayer/FastForwardTransition");
    private Timer                 IndicatorTimer        => _cache.GetNode<Timer>("IndicatorTimer");

    public override Grid Grid { get; set; } = null;

    [Export] public Godot.Collections.Dictionary<ActionEvaluator, double> Evaluators = [];

    [Export] public double CurrentActionCoefficient = 1;

    [Export] public double NextActionCoefficient = 1;

    /// <summary>Sprite to use for the pseudocursor.</summary>
    [Export] public Texture2D CursorSprite
    {
        get => Pseudocursor?.Texture;
        set
        {
            if (Pseudocursor is not null)
                Pseudocursor.Texture = value;
        }
    }

    /// <summary>Pseudocursor sprite offset from the origin of the texture to use for positioning it within a cell.</summary>
    [Export] public Vector2 CursorOffset
    {
        get => Pseudocursor?.Offset ?? Vector2.Zero;
        set
        {
            if (Pseudocursor is not null)
                Pseudocursor.Offset = value;
        }
    }

    /// <summary>Time in seconds to hold the cursor over an indicated cell before acting on it.</summary>
    [Export(PropertyHint.None, "suffix:s")] public float IndicationTime = 0.5f;

    /// <summary>Whether or not this army's turn can be skipped.</summary>
    [Export] public bool EnableTurnSkipping = true;

    /// <summary>Maximum number of levels in the action tree to search for the best action.</summary>
    /// <remarks><b>Warning</b>: Be careful of increasing this higher than 3, or even 2 in some cases, as it can significantly hurt performance.</remarks>
    [Export(PropertyHint.Range, "1,3,or_greater,or_less")] public int MaxSearchDepth = 3;

    /// <summary>Performance option to evaluate moves using threads.</summary>
    /// <remarks>Note: The exact number of threads is based on the C# <see cref="Task"/> pool.</remarks>
    [Export] public bool EvaluateWithThreads = true;

    public AIController() : base() { _cache = new(this); }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = GridData.InvalidCell;
        _action = null;
        _target = null;

        EmitSignal(SignalName.ProgressUpdated, 0, Faction.GetUnits(Grid.Data).Count());
        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip});
    }

    /// <inheritdoc/>
    /// <remarks>Unit actions will still be calculated and the results updated. The screen will be blacked out while computing actions.</remarks>
    public override void FastForwardTurn()
    {
        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
        FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.FastForwardStateChanged, _ff = true), (uint)ConnectFlags.OneShot);
        FastForwardTransition.TransitionOut();
    }

    public override async void SelectUnit(UnitAction[] actions)
    {
        (_selected, _destination, _action, Vector2I target) = await Task.Run(() => ComputeAction(actions));
        if (Grid.Data.Occupants.TryGetValue(target, out UnitData unit))
            _target = unit;
        else
            _target = null;

        EmitSignal(SignalName.UnitSelected, _selected.Cell);
    }

    public override void MoveUnit(UnitData unit, UnitAction[] actions)
    {
        void ConfirmMove() => EmitSignal(SignalName.PathConfirmed, unit.Cell, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, _destination)));
        if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, ConfirmMove, (uint)ConnectFlags.OneShot);
        else
            ConfirmMove();
    }

    public override void CommandUnit(UnitData source, UnitAction[] commands, UnitAction cancel)
    {
        _action ??= commands.FirstOrDefault((a) => a.AIAllowed && !a.RequiresTarget && a.CanPerform(_selected, _destination));
        EmitSignal(SignalName.UnitCommanded, source.Cell, _action);
    }

    public override void SelectTarget(UnitData source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Renderer.Name}'s target has not been determined");
        if (!targets.Contains(_target.Cell))
            throw new InvalidOperationException($"{source.Renderer.Name} can't target {_target.Renderer}");

        Pseudocursor.Position = Grid.PositionOf(_target.Cell);
        Pseudocursor.Visible = true;

        if (_ff)
            EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell);
        else if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell), (uint)ConnectFlags.OneShot);
        else
        {
            IndicatorTimer.Connect(Timer.SignalName.Timeout, () => EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell), (uint)ConnectFlags.OneShot);
            IndicatorTimer.WaitTime = IndicationTime;
            IndicatorTimer.Start();
        }
    }

    public override void FinalizeAction()
    {
        Pseudocursor.Visible = false;
        IEnumerable<UnitData> units = Faction.GetUnits(Grid.Data);
        EmitSignal(
            SignalName.ProgressUpdated,
            units.Count(static (u) => !u.Active) + 1, // Add one to account for the unit that just finished
            units.Count(static (u) => u.Active) - 1
        );
    }

    public override void FinalizeTurn()
    {
        base.FinalizeTurn();
        FastForwardTransition.TransitionIn();
        EmitSignal(SignalName.FastForwardStateChanged, _ff = false);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (!Engine.IsEditorHint() && EnableTurnSkipping && @event.IsActionPressed(InputManager.Cancel))
            EmitSignal(SignalName.TurnFastForward);
    }
}