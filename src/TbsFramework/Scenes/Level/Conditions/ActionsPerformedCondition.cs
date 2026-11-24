using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that is satisfied based on the number of times a set of actions has been performed by a set of units.</summary>
[GlobalClass, Tool]
public partial class ActionsPerformedCondition : GridCondition
{
    /// <summary>Actions to track.</summary>
    [Export] public UnitAction[] Actions = [];

    /// <summary>Units to track. Set to <c>null</c> to count actions performed by all units.</summary>
    /// <remarks><b>Note</b>: Be careful not to individually include units who are part of included factions, or their actions will be counted twice.</remarks>
    [Export] public UnitFilter Units = null;

    /// <summary>Number of times the tracked actions must be performed by the tracked units to satisfy the condition.</summary>
    [Export] public int Amount = 0;

    public override bool IsSatisfied(GridData grid)
    {
        int count;
        if (Units is null)
            count = Actions.Sum(grid.GetTotalPerformances);
        else
        {
            Dictionary<UnitAction, int> performed = grid.GetPerformedActions(Units);
            count = Actions.Sum((a) => performed.GetValueOrDefault(a, 0));
        }
        return count >= Amount;
    }
}