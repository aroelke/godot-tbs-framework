using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Level.Object;

/// <summary>Data structure tracking information about a unit on the map.</summary>
public class UnitData : GridObjectData
{
    /// <summary>Signals that the unit has become active or inactive.</summary>
    public event Action<bool> AvailabilityUpdated;
    /// <summary>Signals that the unit's faction has been changed.</summary>
    public event PropertyChangedEventHandler<Faction> FactionUpdated;
    /// <summary>Signals that the unit's class has been changed.</summary>
    public event PropertyChangedEventHandler<Class> ClassUpdated;
    /// <summary>Signals that the reference to the structure containing the unit's stats has been changed.</summary>
    public event Action<Stats> StatsUpdated;
    /// <summary>Signals that the unit's current health value has changed.</summary>
    public event PropertyChangedEventHandler<double> HealthUpdated
    {
        add    => _health.ValueChanged += value;
        remove => _health.ValueChanged -= value;
    }

    private bool _active = true;
    private Faction _faction = null;
    private Class _class = null;
    private Stats _stats = new();
    private readonly ClampedProperty<double> _health = new(0, double.PositiveInfinity);

    private void Initialize()
    {
        _stats.ValuesChanged += OnStatValuesChanged;
        CellChanged += (from, to) => {
            if ((Grid?.Occupants.TryGetValue(to, out UnitData occupant) ?? false) && occupant != this)
                throw new ArgumentException($"Cell {to} is already occupied");
            if (Grid is not null)
            {
                Grid.Occupants.Remove(from);
                Grid.Occupants[to] = this;
            }
        };
        GridChanged += (from, to) => {
            from?.Occupants.Remove(Cell);
            if (to is not null)
            {
                Cell = to.Clamp(Cell);
                to.Occupants[Cell] = this;
            }
        };
    }

    private void OnStatValuesChanged(Stats stats) => _health.Maximum = stats.Health;

    /// <summary>Whether or not the unit is available to act.</summary>
    public bool Active
    {
        get => _active;
        set
        {
            if (_active != value)
            {
                _active = value;
                if (AvailabilityUpdated is not null)
                    AvailabilityUpdated(_active);
            }
        }
    }

    /// <summary>Faction this unit belongs to.</summary>
    public Faction Faction
    {
        get => _faction;
        set
        {
            if (_faction != value)
            {
                Faction old = _faction;
                _faction = value;
                if (FactionUpdated is not null)
                    FactionUpdated(old, _faction);
            }
        }
    }

    /// <summary>Class this unit has.</summary>
    public Class Class
    {
        get => _class;
        set
        {
            if (_class != value)
            {
                Class old = _class;
                _class = value;
                if (ClassUpdated is not null)
                    ClassUpdated(old, _class);
            }
        }
    }

    /// <summary>This unit's stats.</summary>
    public Stats Stats
    {
        get => _stats;
        set
        {
            if (value is null)
                throw new ArgumentException($"A unit's stats should never be null.");
            if (_stats != value)
            {
                _stats.ValuesChanged -= OnStatValuesChanged;
                _stats = value;
                _stats.ValuesChanged += OnStatValuesChanged;
                if (StatsUpdated is not null)
                    StatsUpdated(_stats);
                _health.Maximum = _stats.Health;
            }
        }
    }

    /// <summary>This unit's current health. Can't go below 0 or above <see cref="Stats.Health"/>.</summary>
    public double Health
    {
        get => _health.Value;
        set => _health.Value = value;
    }

    /// <summary>The unit's behavior if CPU-controlled. Leave <c>null</c> for player-controlled units.</summary>
    public Behavior Behavior = null;

    /// <summary>Reference to the <see cref="Unit"/> rendering the unit's state on the map.</summary>
    public Unit Renderer = null;

    public UnitData() : base()
    {
        _health.Maximum = _stats.Health;
        _health.Value = _stats.Health;

        Initialize();
    }

    private UnitData(UnitData original) : base(original)
    {
        _faction = original._faction;
        _class = original._class;
        _stats = original._stats;
        _health.Maximum = original._health.Maximum;
        _health.Value = original._health.Value;
        Behavior = original.Behavior;

        Initialize();

        Renderer = original.Renderer;
    }

    /// <returns><c>true</c> if this unit can move into or through <paramref name="cell"/>, and <c>false</c> otherwise.</returns>
    public bool IsCellTraversable(Vector2I cell) => !Grid.Occupants.TryGetValue(cell, out UnitData unit) || unit.Faction.AlliedTo(Faction);

    /// <returns>The set of cells this unit can move into or through based on terrain and its movement stat.</returns>
    public IEnumerable<Vector2I> GetTraversableCells()
    {
        int max = 2*(Stats.Move + 1)*(Stats.Move + 1) - 2*Stats.Move - 1;

        Dictionary<Vector2I, int> cells = new(max) {{ Cell, 0 }};
        Queue<Vector2I> potential = new(max);

        potential.Enqueue(Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I neighbor in Grid.GetNeighbors(current))
            {
                int cost = cells[current] + Grid.Terrain.GetValueOrDefault(neighbor, Grid.DefaultTerrain).Cost;
                if ((!cells.TryGetValue(neighbor, out int c) || c > cost) && IsCellTraversable(neighbor) && cost <= Stats.Move) // cost to get to cell is within range
                {
                    cells[neighbor] = cost;
                    potential.Enqueue(neighbor);
                }
            }
        }

        return cells.Keys;
    }

    /// <returns>The set of cells this unit can end its movement in.</returns>
    public IEnumerable<Vector2I> GetOccupiableCells() => GetTraversableCells().Where((c) => !Grid.Occupants.TryGetValue(c, out UnitData occupant) || occupant == this);

    /// <returns>The set of cells this unit can attack from cell <paramref name="source"/>.</returns>
    public IEnumerable<Vector2I> GetAttackableCells(Vector2I source) => Grid.GetCellsInRange(source, Stats.AttackRange);

    /// <returns>The set of cells this unit can attack from its current cell.</returns>
    public IEnumerable<Vector2I> GetAttackableCells() => GetAttackableCells(Cell);

    /// <returns>The set of cells this unit can attack from across all of the cells it can end its movement in.</returns>
    public IEnumerable<Vector2I> GetAttackableCellsInReach() => GetOccupiableCells().SelectMany(GetAttackableCells).ToHashSet();

    /// <returns>The set of cells this unit can attack from across all of the cells it can end its movement in, excluding ones with allies</returns>
    public IEnumerable<Vector2I> GetFilteredAttackableCellsInReach() => GetAttackableCellsInReach().Where((c) => !Grid.Occupants.TryGetValue(c, out UnitData occupant) || !occupant.Faction.AlliedTo(Faction));

    /// <returns>The set of cells this unit can support from cell <paramref name="source"/>.</returns>
    public IEnumerable<Vector2I> GetSupportableCells(Vector2I source) => Grid.GetCellsInRange(source, Stats.SupportRange);

    /// <returns>The set of cells this unit can support from its current cell.</returns>
    public IEnumerable<Vector2I> GetSupportableCells() => GetSupportableCells(Cell);

    /// <returns>The set of cells this unit can support from across all of the cells it can end its movement in.</returns>
    public IEnumerable<Vector2I> GetSupportableCellsInReach() => GetOccupiableCells().SelectMany(GetSupportableCells).ToHashSet();

    /// <returns>The set of cells this unit can support from across all of the cells it can end its movement in, excluding ones with enemies.</returns>
    public IEnumerable<Vector2I> GetFilteredSupportableCellsInReach() => GetSupportableCellsInReach().Where((c) => !Grid.Occupants.TryGetValue(c, out UnitData occupant) || occupant.Faction.AlliedTo(Faction));

    public UnitData Clone() => new(this);
}