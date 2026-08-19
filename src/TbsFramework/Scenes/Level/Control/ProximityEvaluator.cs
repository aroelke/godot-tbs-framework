using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

public enum DistanceStrategy
{
    ManhattanDistance,
    PathLength,
    PathCost
}

[GlobalClass, Tool]
public partial class ProximityEvaluator : ActionEvaluator
{
    [Export] public UnitIdentity[] SpecificUnits = [];

    [Export] public Faction[] InFactions = [];

    [Export] public bool AnyAlly = false;

    [Export] public bool AnyEnemy = false;

    [Export] public bool MoveCloser = true;

    [Export] public DistanceStrategy EvaluationStrategy = DistanceStrategy.ManhattanDistance;

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (SpecificUnits.Contains(u.Identity))
                return true;
            if (InFactions.Contains(u.Faction))
                return true;
            if (AnyAlly && action.Faction.AlliedTo(u.Faction))
                return true;
            if (AnyEnemy && !action.Faction.AlliedTo(u.Faction))
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
                DistanceStrategy.ManhattanDistance => destination.ManhattanDistanceTo(u.Cell),
                DistanceStrategy.PathLength => Path.Empty(action.Grid.AllCells, _ => 1).Add(destination).Add(u.Cell).Count,
                DistanceStrategy.PathCost => actor.PathCost(Path.Empty(action.Grid.AllCells, actor.CellCost).Add(destination).Add(u.Cell)),
                _ => throw new ArgumentOutOfRangeException(PropertyName.EvaluationStrategy)
            });
            if (MoveCloser)
                return 1.0/costs.Min();
            else
                return 1 - 1.0/costs.Min();
        }
    }
}