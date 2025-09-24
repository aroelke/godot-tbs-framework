using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.Scenes.Transitions;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    private const int HealthDiffPrecision = 10;

    private readonly record struct VirtualSpecialActionRegion(SpecialActionRegion Original, StringName Action, ImmutableHashSet<Vector2I> Cells) : ISpecialActionRegion
    {
        ISet<Vector2I> ISpecialActionRegion.Cells => Cells;

        public VirtualSpecialActionRegion(SpecialActionRegion original) : this(original, original.Action, [.. original.GetUsedCells()]) {}

        public bool IsAllowed(IUnit unit) => unit is VirtualUnit u && Original.IsAllowed(u.Original);
    }

    private readonly record struct VirtualGrid(Vector2I Size, Terrain[][] Terrain, IImmutableDictionary<Vector2I, VirtualUnit> Occupants, IImmutableSet<VirtualSpecialActionRegion> Regions) : IGrid
    {
        public VirtualGrid(Vector2I size, Terrain terrain, IImmutableDictionary<Vector2I, VirtualUnit> occupants) : this(size, [.. Enumerable.Repeat(Enumerable.Repeat(terrain, size.X).ToArray(), size.Y)], occupants, []) {}

        public VirtualGrid(Grid grid) : this(
            grid.Size,
            [.. Enumerable.Range(0, grid.Size.Y).Select((r) => Enumerable.Range(0, grid.Size.X).Select((c) => grid.GetTerrain(new(c, r))).ToArray())],
            grid.Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => new VirtualUnit(e.Value as Unit)),
            [.. grid.SpecialActionRegions.Select((r) => new VirtualSpecialActionRegion(r))]
        ) {}

        public IEnumerable<VirtualAction> GetAvailableActions(Faction faction, int specials=0)
        {
            List<VirtualAction> actions = [];
            foreach (VirtualUnit unit in Occupants.Values.Where((u) => u.Faction == faction && u.Active && u.ExpectedHealth > 0))
                foreach (UnitAction action in unit.Original.Behavior.Actions(unit, this))
                        actions.Add(new(this, unit, action.Name, action.Source, action.Target, action.Traversable, specials));
            return actions;
        }

        public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
        public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out VirtualUnit unit) || unit.Faction.AlliedTo(faction);
        public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
        public Terrain GetTerrain(Vector2I cell) => Terrain[cell.Y][cell.X];
        public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
        public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => Occupants.ToImmutableDictionary((e) => e.Key, (e) => e.Value as IUnit);
        public IEnumerable<ISpecialActionRegion> GetSpecialActionRegions() => [.. Regions];
    }

    private readonly record struct VirtualUnit(Unit Original, Vector2I Cell, float ExpectedHealth, bool Active) : IUnit
    {
        public VirtualUnit(Unit original) : this(original, original.Cell, original.Health.Value, original.Active) {}

        public Stats Stats => Original.Stats;
        public Faction Faction => Original.Faction;
        public int Health => (int)Math.Round(ExpectedHealth);

        public IEnumerable<Vector2I> TraversableCells(IGrid grid) => IUnit.TraversableCells(this, grid);
        public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.AttackRange);
        public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, Original.SupportRange);
    }

    private class VirtualAction : IEquatable<VirtualAction>, IComparable<VirtualAction>
    {
        public static bool operator >(VirtualAction a, VirtualAction b) => a.CompareTo(b) > 0;
        public static bool operator <(VirtualAction a, VirtualAction b) => a.CompareTo(b) < 0;

        private VirtualGrid _result;
        private List<VirtualUnit> _enemies = [];
        private readonly HashSet<VirtualUnit> _allies = [];
        private Vector2I _destination = -Vector2I.One;

        public VirtualAction(VirtualGrid initial, VirtualUnit actor, StringName action, IEnumerable<Vector2I> sources, Vector2I target, IEnumerable<Vector2I> traversable, int specials)
        {
            Initial = initial;
            Actor = actor;
            Action = action;
            Sources = sources;
            Target = target;
            Traversable = traversable;
            SpecialActionsPerformed = specials;

            _destination = Actor.Cell;
            Result = Initial;
        }

        public VirtualAction(VirtualAction original, 
            VirtualGrid? initial=null,
            VirtualUnit? actor=null,
            StringName action=null,
            IEnumerable<Vector2I> sources=null,
            Vector2I? target=null,
            IEnumerable<Vector2I> traversable=null,
            Vector2I? destination=null,
            VirtualGrid? result=null,
            int? specials=null
        ) : this(
            initial ?? original.Initial,
            actor ?? original.Actor,
            action ?? original.Action,
            sources ?? original.Sources,
            target ?? original.Target,
            traversable ?? original.Traversable,
            specials ?? original.SpecialActionsPerformed
        )
        {
            Destination = destination ?? original.Destination;
            Result = result ?? original.Result;
        }

        public VirtualGrid Initial { get; private set; }
        public VirtualUnit Actor { get; private set; }
        public StringName Action { get; private set; }
        public IEnumerable<Vector2I> Sources { get; private set; }
        public Vector2I Target { get; private set; }
        public IEnumerable<Vector2I> Traversable { get; private set; }

        public Vector2I Destination
        {
            get => _destination;
            set => PathCost = Path.Empty(Initial, Actor.TraversableCells(Initial)).Add(Actor.Cell).Add(_destination = value).Cost;
        }

        public VirtualGrid Result
        {
            get => _result;
            set
            {
                _result = value;

                foreach ((_, VirtualUnit unit) in _result.Occupants)
                {
                    if (Actor.Faction.AlliedTo(unit.Faction))
                    {
                        _allies.Add(unit);
                        if (unit.ExpectedHealth <= 0)
                            DefeatedAllies++;
                        AllyHealthDifference += unit.Stats.Health - unit.ExpectedHealth;
                    }
                    else
                    {
                        _enemies.Add(unit);
                        if (unit.ExpectedHealth <= 0)
                            DefeatedEnemies++;
                        EnemyHealthDifference += unit.Stats.Health - unit.ExpectedHealth;
                    }
                }

                _enemies.Sort((a, b) => (int)((a.ExpectedHealth - b.ExpectedHealth)*HealthDiffPrecision));
            }
        }

        public int SpecialActionsPerformed = 0;
        public int DefeatedEnemies { get; private set; } = 0;
        public int DefeatedAllies { get; private set; } = 0;
        public float AllyHealthDifference { get; private set; } = 0;
        public float EnemyHealthDifference { get; private set; } = 0;
        public int PathCost { get; private set; } = 0;

        public bool Equivalent(VirtualAction other) => other is not null && Initial == other.Initial && Actor == other.Actor && Action == other.Action && Target == other.Target;

        public bool Equals(VirtualAction other) => Equivalent(other) && Destination == other.Destination;
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
                if (_enemies[i].ExpectedHealth != other._enemies[i].ExpectedHealth)
                    return (int)((other._enemies[i].ExpectedHealth - _enemies[i].ExpectedHealth)*HealthDiffPrecision);

            if ((diff = (int)((EnemyHealthDifference - other.EnemyHealthDifference)*HealthDiffPrecision)) != 0)
                return diff;

            return other.PathCost - PathCost;
        }

        public override string ToString() => $"Move {Actor.Faction.Name}@{Actor.Cell} to {Destination} and {Action} {Target}";
        public override int GetHashCode() => HashCode.Combine(Initial, Actor, Action, Target, Destination);
    }

    private static VirtualAction EvaluateAction(IEnumerable<VirtualAction> actions, VirtualAction action, Dictionary<VirtualGrid, VirtualAction> decisions, int remaining)
    {
        VirtualUnit? target = null;
        HashSet<Vector2I> destinations = [.. action.Sources];
        bool safe = false;
        if (action.Action == UnitAction.AttackAction)
        {
            target = action.Initial.Occupants[action.Target];
            IEnumerable<Vector2I> safeCells = destinations.Where((c) => !target.Value.Original.AttackRange.Contains(c.ManhattanDistanceTo(target.Value.Cell)));
            if (safe = safeCells.Any())
                destinations = [.. safeCells];
        }
        else if (action.Action == UnitAction.SupportAction)
            target = action.Initial.Occupants[action.Target];
        else if (action.Action == UnitAction.EndAction)
            throw new InvalidOperationException($"End actions cannot be evaluated");

        List<Vector2I> choices = [];
        if (destinations.Contains(action.Actor.Cell))
            choices.Add(action.Actor.Cell);
        if (!destinations.Contains(action.Actor.Cell) || destinations.Count > 1)
            choices.Add(destinations.Where((c) => c != action.Actor.Cell).MinBy((c) => action.Initial.PathCost(action.Actor.Original.Behavior.GetPath(action.Actor, action.Initial, c))));

        return choices.Select((c) => {
            VirtualUnit actor;
            VirtualGrid after;
            bool special = false;
            if (action.Action == UnitAction.AttackAction)
            {
                float targetHealth = target.Value.ExpectedHealth, actorHealth = action.Actor.ExpectedHealth;
                static float ExpectedDamage(VirtualUnit a, VirtualUnit b) => Math.Max(0f, a.Original.Stats.Accuracy - b.Original.Stats.Evasion)/100f*(a.Original.Stats.Attack - b.Original.Stats.Defense);
                targetHealth -= ExpectedDamage(action.Actor, target.Value);
                if (targetHealth > 0)
                {
                    if (!safe)
                        actorHealth -= ExpectedDamage(target.Value, action.Actor);
                    if (actorHealth > 0 && action.Actor.Original.Stats.Agility > target.Value.Original.Stats.Agility)
                        targetHealth -= ExpectedDamage(action.Actor, target.Value);
                    else if (targetHealth > 0 && !safe && target.Value.Original.Stats.Agility > action.Actor.Original.Stats.Agility)
                        actorHealth -= ExpectedDamage(target.Value, action.Actor);
                }
                actor = action.Actor with { Cell = c, ExpectedHealth = actorHealth, Active = false };
                VirtualUnit updated = target.Value with { ExpectedHealth = targetHealth };
                after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(c, actor).Remove(target.Value.Cell).Add(updated.Cell, updated) };
            }
            else if (action.Action == UnitAction.SupportAction)
            {
                float targetHealth = target.Value.ExpectedHealth, actorHealth = action.Actor.ExpectedHealth;
                targetHealth = Math.Min(targetHealth + action.Actor.Stats.Healing, target.Value.Stats.Health);
                actor = action.Actor with { Cell = c, ExpectedHealth = actorHealth, Active = false };
                VirtualUnit updated = target.Value with { ExpectedHealth = targetHealth };
                after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(c, actor).Remove(target.Value.Cell).Add(updated.Cell, updated) };
            }
            else
            {
                actor = action.Actor with { Cell = c, Active = false };
                after = action.Initial with { Occupants = action.Initial.Occupants.Remove(action.Actor.Cell).Add(c, actor) };
                special = true;
            }

            if (decisions.TryGetValue(after, out VirtualAction decision))
                return decision;

            VirtualAction result;
            if (remaining == 0 || remaining > 1)
            {
                remaining = Math.Max(0, remaining - 1);
                IEnumerable<VirtualAction> further = after.GetAvailableActions(actor.Faction, special ? action.SpecialActionsPerformed + 1 : action.SpecialActionsPerformed).Where((a) => {
                    if (!actions.Any(a.Equivalent))
                        return true;
                    if (action.Action != UnitAction.AttackAction || a.Action != UnitAction.AttackAction)
                        return true;
                    if (a.Initial.Occupants[a.Target].ExpectedHealth <= 0)
                        return false;
                    if (target.Value.ExpectedHealth <= 0)
                        return true;
                    if (a.Target == action.Target)
                        return true;
                    return false;
                });
                if (further.Any())
                    result = new(action, destination:c, result:further.Select((a) => EvaluateAction(further, a, decisions, remaining)).Max().Result);
                else
                    result = new(action, destination:c, result:after, specials:special ? action.SpecialActionsPerformed + 1 : action.SpecialActionsPerformed);
            }
            else
                result = new(action, destination:c, result:after, specials:special ? action.SpecialActionsPerformed + 1 : action.SpecialActionsPerformed);
            return decisions[after] = result;
        }).Max();
    }

    private (VirtualUnit selected, Vector2I destination, StringName action, Vector2I target) ComputeAction(IEnumerable<VirtualUnit> available, VirtualGrid grid)
    {
        VirtualUnit? selected = null;
        Vector2I destination = -Vector2I.One;
        StringName action = null;
        Vector2I target;

        List<VirtualAction> actions = [.. grid.GetAvailableActions(Army.Faction)];
        if (actions.Count != 0)
        {
            Dictionary<VirtualGrid, VirtualAction> decisions = [];
            VirtualAction result = actions.Select((a) => EvaluateAction(actions, a, decisions, MaxSearchDepth)).Max(); /* Task.WhenAll([.. actions.Select((a) => Task.Run(() => EvaluateAction(a, MaxSearchDepth)))]).Result.Max(); */
            selected = result.Actor;
            destination = result.Destination;
            action = result.Action;
            target = result.Target;
        }
        else
        {
            IEnumerable<VirtualUnit> enemies = grid.GetOccupantUnits().Values.Where((u) => !u.Faction.AlliedTo(Army.Faction)).OfType<VirtualUnit>();

            selected = enemies.Any() ? available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min()) : available.First();
            action = UnitAction.EndAction;

            IEnumerable<VirtualUnit> ordered = enemies.OrderBy((u) => u.Cell.DistanceTo(selected.Value.Cell));
            if (ordered.Any())
            {
                destination = selected.Value.Original.Behavior.Destinations(selected.Value, grid)
                    .OrderBy((c) => selected.Value.Original.Behavior.GetPath(selected.Value, grid, c).Cost)
                    .OrderBy((c) => c.DistanceTo(ordered.First().Cell)).First();
            }
            else
                destination = selected.Value.Cell;
            target = -Vector2I.One;
        }

        return (selected.Value, destination, action, target);
    }

    private Grid _grid = null;
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;
    private bool _ff = false;

    private Sprite2D _pseudocursor = null;
    private Sprite2D Pseudocursor => _pseudocursor ??= GetNode<Sprite2D>("Pseudocursor");

    private FadeToBlackTransition _fft = null;
    private FadeToBlackTransition FastForwardTransition => _fft ??= GetNode<FadeToBlackTransition>("CanvasLayer/FastForwardTransition");

    private TextureProgressBar _progress = null;
    private TextureProgressBar TurnProgress => _progress ??= GetNode<TextureProgressBar>("CanvasLayer/TurnProgress");

    private Timer _indicator = null;
    private Timer IndicatorTimer => _indicator ??= GetNode<Timer>("IndicatorTimer");

    public override Grid Grid { get => _grid; set => _grid = value; }

    /// <summary>Time in seconds to hold the cursor over an indicated cell before acting on it.</summary>
    [Export(PropertyHint.None, "suffix:s")] public float IndicationTime = 0.5f;

    /// <summary>Whether or not this army's turn can be skipped.</summary>
    [Export] public bool EnableTurnSkipping = true;

    /// <summary>Maximum number of levels in the action tree to search for the best action.</summary>
    [Export] public int MaxSearchDepth = 0;

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;

        TurnProgress.MaxValue = ((IEnumerable<Unit>)Army).Count();
        TurnProgress.Value = 0;
    }

    public (Unit selected, Vector2I destination, StringName action, Unit target) ComputeAction(IEnumerable<Unit> available, IEnumerable<Unit> enemies, Grid grid)
    {
        VirtualGrid virtualGrid = new(grid);
        IEnumerable<VirtualUnit> virtualAvailable = available.Select((u) => new VirtualUnit(u));

        (VirtualUnit selected, Vector2I destination, StringName action, Vector2I target) = ComputeAction(virtualAvailable, virtualGrid);

        return (selected.Original, destination, action, virtualGrid.Occupants.TryGetValue(target, out VirtualUnit occupant) ? occupant.Original : null);
    }

    /// <inheritdoc/>
    /// <remarks>Unit actions will still be calculated and the results updated. The screen will be blacked out while computing actions.</remarks>
    public override void FastForwardTurn()
    {
        FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => TurnProgress.Visible = _ff = true, (uint)ConnectFlags.OneShot);
        FastForwardTransition.TransitionOut();
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        VirtualGrid grid = new(Grid);
        IEnumerable<VirtualUnit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active).Select(static (u) => new VirtualUnit(u))];

        (VirtualUnit selected, _destination, _action, Vector2I target) = await Task.Run<(VirtualUnit, Vector2I, StringName, Vector2I)>(() => ComputeAction(available, grid));
        _selected = selected.Original;
        if (grid.Occupants.TryGetValue(target, out VirtualUnit occupant))
            _target = occupant.Original;
        else
            _target = null;

        EmitSignal(SignalName.UnitSelected, _selected);
    }

    public override void MoveUnit(Unit unit)
    {
        void ConfirmMove() => EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, unit.Grid, _destination)));
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
        TurnProgress.MaxValue = ((IEnumerable<Unit>)Army).Count();
        TurnProgress.Value = ((IEnumerable<Unit>)Army).Count((u) => !u.Active) + 1; // Add one to account for the unit that just finished
    }

    public override void FinalizeTurn()
    {
        base.FinalizeTurn();
        FastForwardTransition.TransitionIn();
        TurnProgress.Visible = _ff = false;
    }


    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (EnableTurnSkipping && @event.IsActionPressed(InputManager.Cancel))
            EmitSignal(SignalName.TurnFastForward);
    }
}