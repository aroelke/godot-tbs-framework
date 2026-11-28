using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Conditions;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Evaluates an action based on the actor's proximity to the unit in a specified group that's closest to it.</summary>
[GlobalClass, Tool]
public partial class ProximityEvaluator : ActionEvaluator
{
    private bool _allies = false, _enemies = false;

    /// <summary>Units to count proximity to.</summary>
    [Export] public UnitFilter Included = null;

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

    /// <summary><c>true</c> if being closer to the closest unit in the group results in a high value or <c>false</c> if being further away does.</summary>
    [Export] public bool MoveCloser = true;

    /// <summary>Strategy to use to measure the distance to the closest unit in the group.</summary>
    [Export] public DistanceStrategy EvaluationStrategy = DistanceStrategy.ManhattanDistance;

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (Included?.IsIncluded(u) ?? false)
                return true;
            if (_allies && action.Actor.Faction.AlliedTo(u.Faction))
                return true;
            if (_enemies && !action.Actor.Faction.AlliedTo(u.Faction))
                return true;
            return false;
        }

        IEnumerable<UnitData> included = action.Result.Occupants.Values.Where((u) => u != action.Actor && u.Health > 0 && IsIncluded(u));
        if (!included.Any())
            return 0;
        else
        {
            Vector2I destination = action.Traversed[^1];
            IEnumerable<int> costs = included.Select((u) => EvaluationStrategy.GetCost(destination, u.Cell, action.Result.AllCells, action.Actor));
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
        if (property["name"].AsStringName() == PropertyName.Included && _allies && _enemies)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}