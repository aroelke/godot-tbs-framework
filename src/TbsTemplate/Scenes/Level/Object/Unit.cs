using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Collections.Immutable;
using System;
using TbsTemplate.Data;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Level.Map;

namespace TbsTemplate.Scenes.Level.Object;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and an interface for the player to
/// interact.
/// </summary>
[SceneTree, Tool]
public partial class Unit : GridNode, IHasHealth
{
    // AnimationTree parameters
    private static readonly StringName Idle = "parameters/conditions/idle";
    private static readonly StringName Selected = "parameters/conditions/selected";
    private static readonly StringName Moving = "parameters/conditions/moving";
    private static readonly StringName MoveDirection = "parameters/Moving/Walking/blend_position";
    private static readonly StringName Done = "parameters/conditions/done";

    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private Faction _faction = null;
    private Stats _stats = new();

    /// <summary>Get all cells in a set of ranges from a set of source cells.</summary>
    /// <param name="sources">Cells to compute ranges from.</param>
    /// <param name="ranges">Ranges to compute from <paramref name="sources"/>.</param>
    /// <returns>
    /// The set of all cells that are exactly within <paramref name="ranges"/> distance from at least one element of
    /// <paramref name="sources"/>.
    /// </returns>
    private ImmutableHashSet<Vector2I> GetCellsInRange(IEnumerable<Vector2I> sources, IEnumerable<int> ranges) => sources.SelectMany((c) => ranges.SelectMany((r) => Grid.GetCellsAtRange(c, r))).ToImmutableHashSet();

    private (IEnumerable<Vector2I>, IEnumerable<Vector2I>, IEnumerable<Vector2I>) ExcludeOccupants(IEnumerable<Vector2I> move, IEnumerable<Vector2I> attack, IEnumerable<Vector2I> support)
    {
        IEnumerable<Unit> allies = Grid.Occupants.Select(static (e) => e.Value).OfType<Unit>().Where((u) => Faction.AlliedTo(u));
        IEnumerable<Unit> enemies = Grid.Occupants.Select(static (e) => e.Value).OfType<Unit>().Where((u) => !Faction.AlliedTo(u));
        return (
            move.Where((c) => !enemies.Any((u) => u.Cell == c)),
            attack.Where((c) => !allies.Any((u) => u.Cell == c)),
            support.Where((c) => !enemies.Any((u) => u.Cell == c))
        );
    }

    /// <summary>Class this unit belongs to, defining some of its stats and animations.</summary>
    [Export] public Class Class = null;

    /// <summary>Stats this unit has that determine its movement range and combat performance.</summary>
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

    /// <summary>Faction to which this unit belongs, to determine its allies and enemies.</summary>
    [Export] public Faction Faction
    {
        get => _faction;
        set
        {
            if (_faction != value)
            {
                _faction = value;
                if (Sprite is not null && _faction is not null)
                    Sprite.Modulate = _faction.Color;
            }
        }
    }

    [ExportGroup("Path Traversal", "Move")]

    /// <summary>Base speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

    /// <summary>Factor to multiply <see cref="MoveSpeed"/> by while <see cref="MoveAccelerateAction"/> is held down.</summary>
    [Export] public double MoveAccelerationFactor = 2;

    /// <summary>Whether or not the unit has completed its turn.</summary>
    public bool Active => !AnimationTree.Get(Done).AsBool();

    /// <returns>The set of cells that this unit can reach from its position, accounting for <see cref="Terrain.Cost"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells()
    {
        int max = 2*(Stats.Move + 1)*(Stats.Move + 1) - 2*Stats.Move - 1;

        Dictionary<Vector2I, int> cells = new(max) {{ Cell, 0 }};
        Queue<Vector2I> potential = new(max);

        potential.Enqueue(Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I direction in Vector2IExtensions.Directions)
            {
                Vector2I neighbor = current + direction;
                if (Grid.Contains(neighbor))
                {
                    int cost = cells[current] + Grid.GetTerrain(neighbor).Cost;
                    if ((!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && // cell hasn't been examined yet or this path is shorter to get there
                        Grid.Occupants.GetValueOrDefault(neighbor) switch // cell is empty or contains an allied unit
                        {
                            Unit unit => unit.Faction.AlliedTo(this),
                            null => true,
                            _ => false
                        } &&
                        cost <= Stats.Move) // cost to get to cell is within range
                    {
                        cells[neighbor] = cost;
                        potential.Enqueue(neighbor);
                    }
                }
            }
        }

        return cells.Keys;
    }

    /// <summary>Compute all of the cells this unit could attack from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute attack range from.</param>
    /// <returns>The set of all cells that could be attacked from any of the cell <paramref name="sources"/>.</returns>
    public IEnumerable<Vector2I> AttackableCells(IEnumerable<Vector2I> sources) => GetCellsInRange(sources, AttackRange);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> AttackableCells(Vector2I source) => AttackableCells([source]);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> AttackableCells() => AttackableCells(Cell);

    /// <summary>Compute all of the cells this unit could support from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute support range from.</param>
    /// <returns>The set of all cells that could be supported from any of the source cells.</returns>
    public IEnumerable<Vector2I> SupportableCells(IEnumerable<Vector2I> sources) => GetCellsInRange(sources, SupportRange);

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
        AnimationTree.Set(Idle, false);
        AnimationTree.Set(Selected, true);
        AnimationTree.Set(Moving, false);
    }

    /// <summary>Put the unit in the "idle" state.</summary>
    public void Deselect()
    {
        AnimationTree.Set(Idle, true);
        AnimationTree.Set(Selected, false);
        AnimationTree.Set(Moving, false);
    }

    /// <summary>Put the unit in its "done" state, indicating it isn't available to act anymore.</summary>
    public void Finish()
    {
        Sprite.Modulate = Colors.White;
        AnimationTree.Set(Selected, false);
        AnimationTree.Set(Done, true);
    }

    /// <summary>Restore the unit into its "idle" state from being inactive, indicating that it's ready to act again.</summary>
    public void Refresh()
    {
        Sprite.Modulate = Faction.Color;
        AnimationTree.Set(Done, false);
        AnimationTree.Set(Idle, true);
    }

    /// <summary>Play the unit's death animation and then remove it from the scene.</summary>
    public void Die()
    {
        GD.Print($"Defeated unit ${Name}!");
        Grid.Occupants[Cell] = null;
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
            Cell = path[^1];
            AnimationTree.Set(Selected, false);
            AnimationTree.Set(Moving, true);
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
            UnitEvents.Singleton.EmitSignal(UnitEvents.SignalName.UnitDefeated, this);
    }

    public override void _Ready()
    {
        base._Ready();

        if (_faction is not null)
            Sprite.Modulate = _faction.Color;

        if (!Engine.IsEditorHint())
        {
            Path.Curve = new();
            MotionBox.Size = Size;
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
                Position = Grid.PositionOf(Cell);
                Path.Curve.ClearPoints();
                SetProcess(false);
                EmitSignal(SignalName.DoneMoving);
            }
        }
        else
        {
            if (_faction is not null)
                Sprite.Modulate = _faction.Color;
        }
    }
}