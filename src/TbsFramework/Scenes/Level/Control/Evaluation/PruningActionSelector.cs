using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Breadth-first action selection algorithm that removes all but the N best actions each level of the action tree.</summary>
[GlobalClass, Tool]
public partial class PruningActionSelector : ActionSelector
{
    private record class ActionTreeNode(ResolvedAction Action, double Value, ActionTreeNode Parent = null)
    {
        public ActionTreeNode GetRoot() => Parent?.GetRoot() ?? this;
    }

    private ActionTreeNode CreateNode(ResolvedAction action, ActionTreeNode parent = null) => new(action, GetActionValue(action), parent);

    /// <summary>
    /// Maximum number of actions to retain each level of the tree. If fewer actions are available in a level, all of them will
    /// be retained.
    /// </summary>
    [Export] public int MaxActionsPerRound = 10;

    public override ResolvedAction? SelectAction(IEnumerable<UnitData> units, IEnumerable<UnitAction> available)
    {
        if (!units.Any())
            throw new ArgumentException("No units to select an action for");

        IEnumerable<ActionTreeNode> next = GetAvailableActions(units, available).Select((a) => CreateNode(a));
        if (next.Any())
        {
            IEnumerable<ActionTreeNode> current;
            do
            {
                current = next.OrderByDescending((n) => n.Value);
                next = current.Take(MaxActionsPerRound).SelectMany((n) => GetAvailableActions(n.Action.Actor.Faction.GetUnits(n.Action.Result), available).Select((a) => CreateNode(a, parent:n)));
            } while (next.Any());
            return current.First().GetRoot().Action;
        }
        else
            return null;
    }
}