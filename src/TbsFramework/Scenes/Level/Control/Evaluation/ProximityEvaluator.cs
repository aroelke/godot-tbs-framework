using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Strategy to use when making a decision based on distance between two cells.</summary>
public enum DistanceStrategy
{
    /// <summary>Use the Manhattan distance, or the sum of the differences between cell coordinates.</summary>
    ManhattanDistance,
    /// <summary>Use the number of cells contained in the shortest path between the two cells (accounting for cell cost).</summary>
    PathLength,
    /// <summary>
    /// Use the number of cells contained in teh shortest path between the two cells only accounting for walls (defined as cells
    /// whose cost is greater than the moving unit's movement range).
    /// </summary>
    PathLengthNoTerrain,
    /// <summary>Use the cost of moving along the shortest path between the two cells.</summary>
    PathCost
}

/// <summary>Evaluates an action based on the actor's proximity to the unit in a specified group that's closest to it.</summary>
[GlobalClass, Tool]
public partial class ProximityEvaluator : ActionEvaluator
{
    private bool _allies = false, _enemies = false;

    /// <summary>Measure proximity to any allied unit.</summary>
    [Export] public bool AllAllies
    {
        get => _allies;
        set
        {
            if (Engine.IsEditorHint())
            {
                if (_allies != value)
                {
                    _allies = value;
                    NotifyPropertyListChanged();
                }
            }
            else
                _allies = value;
        }
    }

    /// <summary>Measure proximity to any enemy unit.</summary>
    [Export] public bool AllEnemies
    {
        get => _enemies;
        set
        {
            if (Engine.IsEditorHint())
            {
                if (_enemies != value)
                {
                    _enemies = value;
                    NotifyPropertyListChanged();
                }
            }
            else
                _enemies = value;
        }
    }

    /// <summary>Specific units that are part of the group to measure proximity to.</summary>
    [Export] public UnitIdentity[] Units = [];

    /// <summary>Factions containing units that are part of the group to measure proximity to.</summary>
    [Export] public Faction[] Factions = [];

    /// <summary><c>true</c> if being closer to the closest unit in the group results in a high value or <c>false</c> if being further away does.</summary>
    [Export] public bool MoveCloser = true;

    /// <summary>Strategy to use to measure the distance to the closest unit in the group.</summary>
    [Export] public DistanceStrategy EvaluationStrategy = DistanceStrategy.ManhattanDistance;

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (Units.Contains(u.Identity))
                return true;
            if (Factions.Contains(u.Faction))
                return true;
            if (_allies && action.Faction.AlliedTo(u.Faction))
                return true;
            if (_enemies && !action.Faction.AlliedTo(u.Faction))
                return true;
            return false;
        }

        UnitData actor = action.GetActor();
        IEnumerable<UnitData> included = action.Grid.Occupants.Values.Where((u) => u != actor && u.Health > 0 && IsIncluded(u));
        if (!included.Any())
            return 0;
        else
        {
            Vector2I destination = action.Traversed[^1];
            IEnumerable<int> costs = included.Select((u) => EvaluationStrategy switch {
                DistanceStrategy.ManhattanDistance   => destination.ManhattanDistanceTo(u.Cell),
                DistanceStrategy.PathLength          => Path.Empty(action.Grid.AllCells, actor.CellCost).Add(destination).Add(u.Cell).Count - 1,
                DistanceStrategy.PathLengthNoTerrain => Path.Empty(action.Grid.AllCells, (c) => actor.CellCost(c) > actor.Stats.MoveDistance ? int.MaxValue : 1).Add(destination).Add(u.Cell).Count - 1,
                DistanceStrategy.PathCost            => actor.PathCost(Path.Empty(action.Grid.AllCells, actor.CellCost).Add(destination).Add(u.Cell)),
                _ => throw new ArgumentOutOfRangeException(PropertyName.EvaluationStrategy)
            });
            int cost = costs.Min();
            if (MoveCloser)
                return cost == 0 ? 1.0 : 1.0/cost;
            else
                return cost == 0 ? 0 : 1 - 1.0/cost;
        }
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if ((property["name"].AsStringName() == PropertyName.Factions || property["name"].AsStringName() == PropertyName.Units) && _allies && _enemies)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}