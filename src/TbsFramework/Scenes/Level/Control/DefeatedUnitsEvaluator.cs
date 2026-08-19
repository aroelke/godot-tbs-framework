using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class DefeatedUnitsEvaluator : ActionEvaluator
{
    [Export] public Faction[] Factions = [];

    [Export] public bool AllAllies = false;

    [Export] public bool AllEnemies = false;

    public override double Evaluate(SimulatedAction action)
    {
        bool IsIncluded(UnitData u)
        {
            if (Factions.Contains(u.Faction))
                return true;
            if (AllAllies && action.Faction.AlliedTo(u.Faction))
                return true;
            if (AllEnemies && !action.Faction.AlliedTo(u.Faction))
                return true;
            return false;
        }
        IEnumerable<UnitData> included = action.Grid.Occupants.Values.Where(IsIncluded);
        return ((double)included.Count((u) => u.Health <= 0))/included.Count();
    }
}