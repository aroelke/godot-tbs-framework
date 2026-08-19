using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class HealthDifferenceEvaluator : ActionEvaluator
{
    [Export] public Faction[] Factions = [];

    [Export] public bool AllAllies = false;

    [Export] public bool AllEnemies = false;

    [Export] public bool IncludeDefeated = true;

    [Export(PropertyHint.Range, "0,5,1,or_greater")] public int TakeLargest = 0;

    [Export(PropertyHint.Range, "0,1,0.01")] public double NoUnitsValue = 0;

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (!IncludeDefeated && u.Health <= 0)
                return false;
            if (Factions.Contains(u.Faction))
                return true;
            if (AllAllies && action.Faction.AlliedTo(u.Faction))
                return true;
            if (AllEnemies && !action.Faction.AlliedTo(u.Faction))
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
}