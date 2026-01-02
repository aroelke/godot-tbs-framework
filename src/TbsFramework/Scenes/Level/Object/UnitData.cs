using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Level.Object;

public class UnitData : GridObjectData
{
    public delegate void FactionUpdatedEventHandler(Faction faction);
    public delegate void ClassUpdatedEventHandler(Class @class);
    public delegate void StatsUpdatedEventHandler(Stats stats);
    public delegate void HealthUpdatedEventHandler(double hp);

    public event FactionUpdatedEventHandler FactionUpdated;
    public event ClassUpdatedEventHandler ClassUpdated;
    public event StatsUpdatedEventHandler StatsUpdated;
    public event HealthUpdatedEventHandler HealthUpdated;

    private Faction _faction = null;
    private Class _class = null;
    private Stats _stats = new();
    private double _hp = 0;

    public bool Active = true;

    public Faction Faction
    {
        get => _faction;
        set
        {
            if (_faction != value)
            {
                _faction = value;
                if (FactionUpdated is not null)
                    FactionUpdated(_faction);
            }
        }
    }

    public Class Class
    {
        get => _class;
        set
        {
            if (_class != value)
            {
                _class = value;
                if (ClassUpdated is not null)
                    ClassUpdated(_class);
            }
        }
    }

    public Stats Stats
    {
        get => _stats;
        set
        {
            if (value is null)
                throw new ArgumentException($"A unit's stats should never be null.");
            if (_stats != value)
            {
                _stats = value;
                if (StatsUpdated is not null)
                    StatsUpdated(_stats);
            }
        }
    }

    public double Health
    {
        get => _hp;
        set
        {
            double next = Mathf.Clamp(value, 0, _stats.Health);
            if (_hp != next)
            {
                _hp = next;
                if (HealthUpdated is not null)
                    HealthUpdated(_hp);
            }
        }
    }

    public Behavior Behavior = null;

    public UnitData() : base(true)
    {
        _hp = Stats.Health;
    }

    private UnitData(UnitData original) : base(true)
    {
        _faction = original._faction;
        _class = original._class;
        _stats = original._stats;
        _hp = original._hp;
        Cell = original.Cell;
        Grid = original.Grid;
    }

    public bool IsCellTraversable(Vector2I cell) => !Grid.Occupants.TryGetValue(cell, out GridObjectData occupant) || (occupant is UnitData unit && unit.Faction.AlliedTo(Faction));

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

    public IEnumerable<Vector2I> GetAttackableCells(Vector2I source) => Grid.GetCellsInRange(source, Stats.AttackRange);

    public IEnumerable<Vector2I> GetAttackableCells() => GetAttackableCells(Cell);

    public IEnumerable<Vector2I> GetAttackableCellsInReach() => GetTraversableCells().SelectMany(GetAttackableCells).ToHashSet();

    public IEnumerable<Vector2I> GetSupportableCells(Vector2I source) => Grid.GetCellsInRange(source, Stats.SupportRange);

    public IEnumerable<Vector2I> GetSupportableCells() => GetSupportableCells(Cell);

    public IEnumerable<Vector2I> GetSupportableCellsInReach() => GetTraversableCells().SelectMany(GetSupportableCells).ToHashSet();

    public override GridObjectData Clone() => new UnitData(this);
}