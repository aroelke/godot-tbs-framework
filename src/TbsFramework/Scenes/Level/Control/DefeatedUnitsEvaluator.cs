using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Evaluates actions based on the number of defeated units on the grid as the result of an action.</summary>
[GlobalClass, Tool]
public partial class DefeatedUnitsEvaluator : ActionEvaluator
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

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (Factions.Contains(u.Faction))
                return true;
            if (_allies && action.Faction.AlliedTo(u.Faction))
                return true;
            if (_enemies && !action.Faction.AlliedTo(u.Faction))
                return true;
            return false;
        }
        IEnumerable<UnitData> included = action.Grid.Occupants.Values.Where(IsIncluded);
        return ((double)included.Count((u) => u.Health <= 0))/included.Count();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].AsStringName() == PropertyName.Factions && _allies && _enemies)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}