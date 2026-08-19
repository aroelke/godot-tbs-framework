using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Evaluates a grid based on the total health lost by a collection of units.</summary>
[GlobalClass, Tool]
public partial class HealthDifferenceEvaluator : ActionEvaluator
{
    private bool _allies = false, _enemies = false;

    /// <summary><c>true</c> if all units in allied factions should be counted.</summary>
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

    /// <summary><c>true</c> if all units in enemy factions should be counted.</summary>
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

    /// <summary>Factions containing units to count.</summary>
    [Export] public Faction[] Factions = [];

    /// <summary><c>true</c> if units that were defeated should be counted for total health lost.</summary>
    [Export] public bool IncludeDefeated = true;

    /// <summary>
    /// Only count a number of units with the most health lost. If there are fewer than that many units in the group,
    /// count all of them.
    /// </summary>
    [Export(PropertyHint.Range, "0,5,1,or_greater")] public int TakeLargest = 0;

    /// <summary>If there are no units in the included group, use this value to evaluate the grid. Should be between 0 and 1.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public double NoUnitsValue = 0;

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (!IncludeDefeated && u.Health <= 0)
                return false;
            if (Factions.Contains(u.Faction))
                return true;
            if (_allies && action.Faction.AlliedTo(u.Faction))
                return true;
            if (_enemies && !action.Faction.AlliedTo(u.Faction))
                return true;
            return false;
        }

        IEnumerable<UnitData> included = action.Grid.Occupants.Values.Where(IsIncluded);
        if (!included.Any())
            return NoUnitsValue;
        else
        {
            if (TakeLargest > 0 && included.Count() > TakeLargest)
                included = included.OrderByDescending((u) => u.Stats.MaxHealth - u.Health).Take(TakeLargest);
            return included.Sum((u) => u.Stats.MaxHealth - u.Health)/included.Sum((u) => u.Stats.MaxHealth);
        }
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].AsStringName() == PropertyName.Factions && _allies && _enemies)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}