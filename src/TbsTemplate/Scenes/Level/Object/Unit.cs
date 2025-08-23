using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Collections.Immutable;
using System;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Control;

namespace TbsTemplate.Scenes.Level.Object;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and an interface for the player to
/// interact.
/// </summary>
[GlobalClass, SceneTree, Tool]
public partial class Unit : GridNode, IUnit, IHasHealth
{
    // AnimationTree parameters
    private static readonly StringName Idle = "parameters/conditions/idle";
    private static readonly StringName Selected = "parameters/conditions/selected";
    private static readonly StringName Moving = "parameters/conditions/moving";
    private static readonly StringName MoveDirection = "parameters/Moving/Walking/blend_position";
    private static readonly StringName Done = "parameters/conditions/done";

    // AnimationTree states
    private static readonly StringName IdleState = "Idle";
    private static readonly StringName SelectedState = "Selected";
    private static readonly StringName MovingState = "Moving";
    private static readonly StringName DoneState = "Done";

    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private Army _army = null;
    private Stats _stats = new();
    private Vector2I _target = Vector2I.Zero;

    private StringName AnimationState => AnimationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>().GetCurrentNode();

    private void CheckInvalidState(string operation, params StringName[] illegal)
    {
        if (illegal.Contains(AnimationState))
            throw new InvalidOperationException($"Cannot {operation} unit {Name} while in animation state {AnimationState}");
    }

    private void CheckValidState(string operation, params StringName[] legal)
    {
        if (!legal.Contains(AnimationState))
            throw new InvalidOperationException($"Cannot {operation} unit {Name} while in animation state {AnimationState}");
    }

    private (IEnumerable<Vector2I>, IEnumerable<Vector2I>, IEnumerable<Vector2I>) ExcludeOccupants(IEnumerable<Vector2I> move, IEnumerable<Vector2I> attack, IEnumerable<Vector2I> support)
    {
        IEnumerable<Unit> allies = Grid.Occupants.Select(static (e) => e.Value).OfType<Unit>().Where(Army.Faction.AlliedTo);
        IEnumerable<Unit> enemies = Grid.Occupants.Select(static (e) => e.Value).OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u));
        return (
            move.Where((c) => !enemies.Any((u) => u.Cell == c)),
            attack.Where((c) => !allies.Any((u) => u.Cell == c)),
            support.Where((c) => !enemies.Any((u) => u.Cell == c))
        );
    }

    /// <summary>Class this unit belongs to, defining some of its stats and animations.</summary>
    [Export] public Class Class = null;

    [Export] public Stats Stats
    {
        get => _stats;
        set
        {
            if (_stats != value)
            {
                _stats = value;
                if (Health is not null)
                    Health.Maximum = _stats.Health;
            }
        }
    }

    /// <summary>Distances from the unit's occupied cell that it can attack.</summary>
    [Export] public int[] AttackRange = [1, 2];

    /// <summary>Distances from the unit's occupied cell that it can support.</summary>
    [Export] public int[] SupportRange = [1, 2, 3];

    [ExportGroup("Path Traversal", "Move")]

    /// <summary>Base speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

    /// <summary>Factor to multiply <see cref="MoveSpeed"/> by while <see cref="MoveAccelerateAction"/> is held down.</summary>
    [Export] public double MoveAccelerationFactor = 2;

    ///<summary>Behavior defining actions to take when AI controlled.</summary>
    public Behavior Behavior { get; private set; } = null;

    int IUnit.Health => Health.Value;

    /// <summary>Army to which this unit belongs, which determines its alliances and gives access to its compatriots.</summary>
    public Army Army
    {
        get => _army;
        set
        {
            if (_army != value)
            {
                _army = value;
                if (Sprite is not null && _army is not null)
                    Sprite.Modulate = _army.Faction.Color;
            }
        }
    }

    public Faction Faction => Army.Faction;

    /// <summary>Whether or not the unit has completed its turn.</summary>
    public bool Active => !AnimationTree.Get(Done).AsBool();

    public IEnumerable<Vector2I> TraversableCells(IGrid grid) => IUnit.TraversableCells(this, grid);

    /// <returns>The set of cells that this unit can reach from its position, accounting for <see cref="Terrain.Cost"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells() => TraversableCells(Grid);

    public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, AttackRange);

    /// <summary>Compute all of the cells this unit could attack from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute attack range from.</param>
    /// <returns>The set of all cells that could be attacked from any of the cell <paramref name="sources"/>.</returns>
    public IEnumerable<Vector2I> AttackableCells(IEnumerable<Vector2I> sources) => AttackableCells(Grid, sources);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> AttackableCells(Vector2I source) => AttackableCells([source]);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> AttackableCells() => AttackableCells(Cell);

    public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => IUnit.GetCellsInRange(grid, sources, SupportRange);

    /// <summary>Compute all of the cells this unit could support from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute support range from.</param>
    /// <returns>The set of all cells that could be supported from any of the source cells.</returns>
    public IEnumerable<Vector2I> SupportableCells(IEnumerable<Vector2I> sources) => SupportableCells(Grid, sources);

    /// <inheritdoc cref="SupportableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> SupportableCells(Vector2I source) => SupportableCells([source]);

    /// <inheritdoc cref="SupportableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> SupportableCells() => SupportableCells(Cell);

    /// <returns>The complete sets of cells this unit can act on.</returns>
    public (IEnumerable<Vector2I> traversable, IEnumerable<Vector2I> attackable, IEnumerable<Vector2I> supportable) ActionRanges()
    {
        IEnumerable<Vector2I> traversable = TraversableCells();
        return ExcludeOccupants(
            traversable,
            AttackableCells(traversable.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == this)),
            SupportableCells(traversable.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == this))
        );
    }

    /// <summary>Put the unit in the "selected" state.</summary>
    public void Select()
    {
        CheckInvalidState("select", DoneState);
        AnimationTree.Set(Idle, false);
        AnimationTree.Set(Selected, true);
        AnimationTree.Set(Moving, false);
    }

    /// <summary>Put the unit in the "idle" state.</summary>
    public void Deselect()
    {
        CheckInvalidState("deselect", IdleState);
        AnimationTree.Set(Idle, true);
        AnimationTree.Set(Selected, false);
        AnimationTree.Set(Moving, false);
    }

    /// <summary>Put the unit in its "done" state, indicating it isn't available to act anymore.</summary>
    public void Finish()
    {
        Sprite.Modulate = Colors.White;
        AnimationTree.Set(Idle, false);
        AnimationTree.Set(Selected, false);
        AnimationTree.Set(Moving, false);
        AnimationTree.Set(Done, true);
    }

    /// <summary>Restore the unit into its "idle" state from being inactive, indicating that it's ready to act again.</summary>
    public void Refresh()
    {
        CheckValidState("refresh", "", DoneState);
        Sprite.Modulate = Army.Faction.Color;
        AnimationTree.Set(Done, false);
        AnimationTree.Set(Idle, true);
    }

    /// <summary>Play the unit's death animation and then remove it from the scene.</summary>
    public void Die()
    {
        Grid.Occupants.Remove(Cell);
        QueueFree();
    }

    /// <summary>Move the unit along a path of <see cref="Grid"/> cells.</summary>
    /// <param name="path">Coordinates of the cells to move along.</param>
    public void MoveAlong(Path path)
    {
        if (path[0] != Cell)
            throw new ArgumentException("The first cell in the path must be the unit's cell");

        if (path.Count == 1)
            EmitSignal(SignalName.DoneMoving);
        else if (path.Count > 1)
        {
            foreach (Vector2I cell in path)
                Path.Curve.AddPoint(Grid.PositionOf(cell) - Position);
            AnimationTree.Set(Selected, false);
            AnimationTree.Set(Moving, true);
            _target = path[^1];
            SetProcess(true);
        }
    }

    /// <summary>If this unit is moving, skip straight to the end of the path.</summary>
    /// <exception cref="InvalidOperationException">If the unit is not moving.</exception>
    public void SkipMoving()
    {
        if (!AnimationTree.Get(Moving).AsBool())
            throw new InvalidOperationException($"Unit {Name} isn't moving");
        PathFollow.ProgressRatio = 1;
    }

    public void OnHealthChanged(int value)
    {
        if (value == 0)
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitDefeated, this);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (!GetChildren().OfType<Behavior>().Any() && (!GetParentOrNull<Army>()?.GetChildren().OfType<PlayerController>().Any() ?? false))
            warnings.Add("This unit has no behavior. It may not be able to act.");
        if (GetChildren().OfType<Behavior>().Count() > 1)
            warnings.Add("More than one behavior is defined. Only the first one will be used.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (_army is not null)
            Sprite.Modulate = _army.Faction.Color;
        Behavior = GetChildren().OfType<Behavior>().FirstOrDefault();

        if (!Engine.IsEditorHint())
        {
            Path.Curve = new();
            MotionBox.Size = Size;
            AnimationTree.Active = true;
            SetProcess(false);
        }

        Health.Value = Health.Maximum = Stats.Health;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            Vector2 prev = PathFollow.Position;
            PathFollow.Progress += (float)(MoveSpeed*(Accelerate.Active ? MoveAccelerationFactor : 1)*delta);
            Vector2 change = PathFollow.Position - prev;
            if (change != Vector2.Zero)
                AnimationTree.Set(MoveDirection, change);

            if (PathFollow.ProgressRatio >= 1)
            {
                AnimationTree.Set(Selected, true);
                AnimationTree.Set(Moving, false);
                PathFollow.Progress = 0;
                Cell = _target;
                Path.Curve.ClearPoints();
                SetProcess(false);
                EmitSignal(SignalName.DoneMoving);
            }
        }
        else
        {
            if (_army is not null)
                Sprite.Modulate = _army.Faction.Color;
        }
    }
}