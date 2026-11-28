using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Conditions;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Evaluates actions based on the number of defeated units on the grid as the result of an action.</summary>
[GlobalClass, Tool]
public partial class DefeatedUnitsEvaluator : ActionEvaluator
{
    private bool _allies = false, _enemies = false;

    [Export] public UnitFilter Included = null;

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
        double alive = action.Result.Occupants.Values.Count(IsIncluded);
        double dead = action.Result.Removed.Count((u) => IsIncluded(u) && u.Health == 0);
        return (dead > 0) ? dead/(alive + dead) : 0;
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].AsStringName() == PropertyName.Included && _allies && _enemies)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}