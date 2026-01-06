using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Data;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Combat.Data;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Object.Group;
using TbsFramework.Scenes.Transitions;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
[Icon("res://icons/AIController.svg"), Tool]
public partial class AIController : ArmyController
{
    private const int HealthDiffPrecision = 10;

    /// <summary>Signals that the fast-forward state has changed.</summary>
    /// <param name="enable"><c>true</c> if fast-forwarding is in progress, and <c>false</c> otherwise.</param>
    [Signal] public delegate void FastForwardStateChangedEventHandler(bool enable);

    private class VirtualAction : IEquatable<VirtualAction>, IComparable<VirtualAction>
    {
        public static bool operator >(VirtualAction a, VirtualAction b) => a.CompareTo(b) > 0;
        public static bool operator <(VirtualAction a, VirtualAction b) => a.CompareTo(b) < 0;

        private readonly List<UnitData> _enemies = [];
        private readonly HashSet<UnitData> _allies = [];
        private Vector2I _destination = -Vector2I.One;
        private GridData _result = null;

        public VirtualAction(UnitData actor, StringName action, IEnumerable<Vector2I> sources, Vector2I target)
        {
            Actor = actor;
            Action = action;
            Sources = sources;
            Target = target;

            if (Actor is not null)
            {
                Traversable = Actor.GetTraversableCells();
                _destination = Actor.Cell;
            }
        }

        private VirtualAction(VirtualAction original) : this(null, original.Action, original.Sources, original.Target)
        {
            Actor = original.Actor.Grid.Clone().Occupants[original.Actor.Cell] as UnitData;
            Traversable = original.Traversable;
            Destination = original.Destination;

            _result = original.Result;
            SpecialActionsPerformed = original.SpecialActionsPerformed;
            DefeatedEnemies = original.DefeatedEnemies;
            DefeatedAllies = original.DefeatedAllies;
            AllyHealthDifference = original.AllyHealthDifference;
            EnemyHealthDifference = original.EnemyHealthDifference;
            RemainingActions = original.RemainingActions;
        }

        public UnitData Actor { get; private set; }
        public StringName Action { get; private set; }
        public IEnumerable<Vector2I> Sources = [];
        public Vector2I Target = -Vector2I.One;
        public IEnumerable<Vector2I> Traversable { get; private set; }

        public Vector2I Destination
        {
            get => _destination;
            set
            {
                _destination = value;
                PathCost = Path.Empty(Actor.Grid, Actor.GetTraversableCells()).Add(Actor.Cell).Add(_destination).Cost;
            }
        }

        public int SpecialActionsPerformed = 0;
        public int DefeatedEnemies = 0;
        public int DefeatedAllies = 0;
        public double AllyHealthDifference { get; private set; } = 0;
        public double EnemyHealthDifference { get; private set; } = 0;
        public int PathCost { get; private set; } = 0;
        public int RemainingActions = 0;

        public GridData Result
        {
            get => _result;
            set
            {
                _result = value;

                foreach ((_, GridObjectData obj) in _result.Occupants)
                {
                    if (obj is UnitData unit)
                    {
                        if (Actor.Faction.AlliedTo(unit.Faction))
                        {
                            _allies.Add(unit);
                            if (unit.Health <= 0)
                                DefeatedAllies++;
                            AllyHealthDifference += unit.Stats.Health - unit.Health;
                        }
                        else
                        {
                            _enemies.Add(unit);
                            if (unit.Health <= 0)
                                DefeatedEnemies++;
                            EnemyHealthDifference += unit.Stats.Health - unit.Health;
                        }
                    }
                }

                _enemies.Sort((a, b) => (int)((a.Health - b.Health)*HealthDiffPrecision));
            }
        }

        public VirtualAction Clone() => new(this);

        public bool Equals(VirtualAction other) => other is not null && Actor == other.Actor && Actor.Grid == other.Actor.Grid && Action == other.Action && Target == other.Target && Destination == other.Destination;
        public override bool Equals(object obj) => Equals(obj as VirtualAction);

        // Positive is better
        public int CompareTo(VirtualAction other)
        {
            int diff;

            if ((diff = SpecialActionsPerformed - other.SpecialActionsPerformed) != 0)
                return diff;

            if ((diff = DefeatedEnemies - other.DefeatedEnemies) != 0)
                return diff;
            if ((diff = other.DefeatedAllies - DefeatedAllies) != 0)
                return diff;

            if ((diff = (int)((other.AllyHealthDifference - AllyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            int smaller = Math.Min(_enemies.Count, other._enemies.Count);
            for (int i = 0; i < smaller; i++)
                if (_enemies[i].Health != other._enemies[i].Health)
                    return (int)((other._enemies[i].Health - _enemies[i].Health)*HealthDiffPrecision);

            if ((diff = (int)((EnemyHealthDifference - other.EnemyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            if ((diff = RemainingActions - other.RemainingActions) != 0)
                return diff;

            return other.PathCost - PathCost;
        }

        public override string ToString() => $"Move {Actor.Faction.Name}@{Actor.Cell} to {Destination} and {Action} {Target}";
        public override int GetHashCode() => HashCode.Combine(Actor, Actor.Grid, Action, Target, Destination);
    }

    private static List<VirtualAction> GetAvailableActions(GridData grid, Faction faction)
    {
        List<VirtualAction> actions = [];
        foreach ((_, GridObjectData obj) in grid.Occupants)
        {
            if (obj is UnitData unit && unit.Faction == faction && unit.Active && unit.Health > 0)
            {
                foreach (UnitAction action in unit.Behavior.Actions(unit))
                    actions.Add(new(unit, action.Name, action.Source, action.Target));
            }
        }
        return actions;
    }

    private static VirtualAction EvaluateAction(IEnumerable<VirtualAction> actions, VirtualAction action, Dictionary<GridData, VirtualAction> decisions, int remaining)
    {
        UnitData target = null;
        HashSet<Vector2I> destinations = [.. action.Sources];
        if (action.Action == UnitAction.AttackAction)
        {
            target = action.Actor.Grid.Occupants[action.Target] as UnitData;
            IEnumerable<Vector2I> safeCells = destinations.Where((c) => !target.Stats.AttackRange.Contains(c.ManhattanDistanceTo(target.Cell)));
            if (safeCells.Any())
                destinations = [.. safeCells];
        }
        else if (action.Action == UnitAction.SupportAction)
            target = action.Actor.Grid.Occupants[action.Target] as UnitData;
        else if (action.Action == UnitAction.EndAction)
            throw new InvalidOperationException($"End actions cannot be evaluated");

        List<Vector2I> choices = [];
        if (destinations.Contains(action.Actor.Cell))
            choices.Add(action.Actor.Cell);
        if (!destinations.Contains(action.Actor.Cell) || destinations.Count > 1)
            choices.Add(destinations.Where((c) => c != action.Actor.Cell).MinBy((c) => c.ManhattanDistanceTo(action.Actor.Cell)));

        return choices.Max((c) => {
            VirtualAction duplicate = action.Clone();
            duplicate.Actor.Cell = duplicate.Destination = c;
            UnitData target = null;

            if (duplicate.Action == UnitAction.AttackAction)
            {
                target = duplicate.Actor.Grid.Occupants[duplicate.Target] as UnitData;
                List<CombatAction> attacks = CombatCalculations.AttackResults(duplicate.Actor, target, true);
                foreach (CombatAction attack in attacks)
                    attack.Target.Health -= attack.Damage;
            }
            else if (duplicate.Action == UnitAction.SupportAction)
            {
                target = duplicate.Actor.Grid.Occupants[duplicate.Target] as UnitData;
                CombatAction support = CombatCalculations.CreateSupportAction(duplicate.Actor, target);
                support.Target.Health += -support.Damage;
            }
            else
                duplicate.SpecialActionsPerformed++;
            duplicate.Result = duplicate.Actor.Grid;

            // If this action results in a board state that was already explored, skip the rest of this branch and use that result
            if (decisions.TryGetValue(duplicate.Actor.Grid, out VirtualAction decision))
                return decision;

            IEnumerable<VirtualAction> further = GetAvailableActions(duplicate.Result, duplicate.Actor.Faction);
            if (remaining == 0 || remaining > 1)
            {
                remaining = Math.Max(0, remaining - 1);
                IEnumerable<VirtualAction> reduced = further.Where((a) => {
                    // Evaluate a if a was not present in the previous set of actions that were evaluated (the other ones will either reappear or be evaluated later)
                    if (!actions.Any((b) => a.Actor == b.Actor && a.Target == b.Target))
                        return true;
                    // Evaluate a if a or this action was not an attack action
                    if (duplicate.Action != UnitAction.AttackAction || a.Action != UnitAction.AttackAction)
                        return true;
                    // Don't evaluate a if a's target is defeated
                    if ((a.Actor.Grid.Occupants[a.Target] as UnitData).Health <= 0)
                        return false;
                    // Evaluate a if this action defeated its target to see if more enemies can be defeated down this branch
                    if (target.Health <= 0)
                        return true;
                    // Evaluate a if it has the same target as this action
                    if (a.Target == duplicate.Target)
                        return true;
                    return false;
                });
                if (reduced.Any())
                {
                    decisions[duplicate.Actor.Grid] = duplicate.Clone();
                    IEnumerable<VirtualAction> results = reduced.Select((a) => EvaluateAction(reduced, a, decisions, remaining));
                    decisions[duplicate.Actor.Grid].Result = results.Max().Result;
                }
            }
            if (!decisions.TryGetValue(duplicate.Actor.Grid, out VirtualAction value))
            {
                value = duplicate.Clone();
                decisions[duplicate.Actor.Grid] = value;
                decisions[duplicate.Actor.Grid].RemainingActions = further.Count();
            }
            return value;
        });
    }

    private (UnitData selected, Vector2I destination, StringName action, Vector2I target) ComputeAction(IEnumerable<UnitData> available)
    {
        UnitData selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        Vector2I target;

        List<VirtualAction> actions = [.. GetAvailableActions(Grid.Data, Army.Faction)];
        if (actions.Count != 0)
        {
            VirtualAction result;
            if (EvaluateWithThreads)
                result = Task.WhenAll([.. actions.Select((a) => Task.Run(() => EvaluateAction(actions, a, [], MaxSearchDepth)))]).Result.Max();
            else
            {
                Dictionary<GridData, VirtualAction> decisions = [];
                result = actions.Max((a) => EvaluateAction(actions, a, decisions, MaxSearchDepth));
            }
            selected = result.Actor;
            destination = result.Destination;
            action = result.Action;
            target = result.Target;
        }
        else
        {
            IEnumerable<UnitData> enemies = Grid.Data.Occupants.Values.Where((o) => o is UnitData u && !u.Faction.AlliedTo(Army.Faction)).OfType<UnitData>();

            selected = enemies.Any() ? available.MinBy((u) => enemies.Min((e) => u.Cell.DistanceTo(e.Cell))) : available.First();
            action = UnitAction.EndAction;

            IEnumerable<UnitData> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Cell));
            if (ordered.Any())
                destination = selected.Behavior.Destinations(selected).OrderBy((c) => selected.Behavior.GetPath(selected, c).Cost).OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            else
                destination = selected.Cell;
            target = -Vector2I.One;
        }

        return (selected, destination, action, target);
    }

    private readonly NodeCache _cache = null;
    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;
    private bool _ff = false;

    private Sprite2D              Pseudocursor          => _cache.GetNode<Sprite2D>("Pseudocursor");
    private FadeToBlackTransition FastForwardTransition => _cache.GetNode<FadeToBlackTransition>("CanvasLayer/FastForwardTransition");
    private Timer                 IndicatorTimer        => _cache.GetNode<Timer>("IndicatorTimer");

    public override Grid Grid { get => _grid; set => _grid = value; }

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
        _destination = -Vector2I.One;
        _action = null;
        _target = null;

        EmitSignal(SignalName.ProgressUpdated, 0, ((IEnumerable<Unit>)Army).Count());
        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip});
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies, Grid grid)
    {
        (UnitData selected, Vector2I destination, StringName action, Vector2I target) = ComputeAction(available.Select(static (u) => u.UnitData));
        return (selected.Renderer, destination, action, Grid.Data.Occupants.TryGetValue(target, out GridObjectData occupant) && occupant is UnitData unit ? unit.Renderer : null);
    }

    /// <inheritdoc/>
    /// <remarks>Unit actions will still be calculated and the results updated. The screen will be blacked out while computing actions.</remarks>
    public override void FastForwardTurn()
    {
        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
        FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.FastForwardStateChanged, _ff = true), (uint)ConnectFlags.OneShot);
        FastForwardTransition.TransitionOut();
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluate it rather than waiting until later, because that will be in the
        // wrong thread.
        IEnumerable<UnitData> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.UnitData.Active).Select(static (u) => u.UnitData)];

        (UnitData selected, _destination, _action, Vector2I target) = await Task.Run<(UnitData, Vector2I, StringName, Vector2I)>(() => ComputeAction(available));
        _selected = selected.Renderer;
        if (Grid.Data.Occupants.TryGetValue(target, out GridObjectData occupant) && occupant is UnitData unit)
            _target = unit.Renderer;
        else
            _target = null;

        EmitSignal(SignalName.UnitSelected, _selected);
    }

    public override void MoveUnit(Unit unit)
    {
        void ConfirmMove() => EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.UnitData.Behavior.GetPath(unit.UnitData, _destination)));
        if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, ConfirmMove, (uint)ConnectFlags.OneShot);
        else
            ConfirmMove();
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        EmitSignal(SignalName.UnitCommanded, source, _action);
    }

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Name}'s target has not been determined");
        if (!targets.Contains(_target.Cell))
            throw new InvalidOperationException($"{source.Name} can't target {_target}");

        Pseudocursor.Position = Grid.PositionOf(_target.Cell);
        Pseudocursor.Visible = true;

        if (_ff)
            EmitSignal(SignalName.TargetChosen, source, _target);
        else if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.TargetChosen, source, _target), (uint)ConnectFlags.OneShot);
        else
        {
            IndicatorTimer.Connect(Timer.SignalName.Timeout, () => EmitSignal(SignalName.TargetChosen, source, _target), (uint)ConnectFlags.OneShot);
            IndicatorTimer.WaitTime = IndicationTime;
            IndicatorTimer.Start();
        }
    }

    public override void FinalizeAction()
    {
        Pseudocursor.Visible = false;
        EmitSignal(
            SignalName.ProgressUpdated,
            ((IEnumerable<Unit>)Army).Count(static (u) => !u.UnitData.Active) + 1, // Add one to account for the unit that just finished
            ((IEnumerable<Unit>)Army).Count(static (u) => u.UnitData.Active) - 1
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