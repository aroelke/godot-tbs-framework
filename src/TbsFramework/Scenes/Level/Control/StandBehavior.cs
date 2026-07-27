using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Unit behavior that prevents a unit from moving and can optionally prevent actions as well.</summary>
[Tool]
public partial class StandBehavior : Behavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    /// <summary>Whether or not the unit should support allies in range.</summary>
    [Export] public bool SupportInRange = true;

    public override IEnumerable<Vector2I> Destinations(UnitData unit) => [unit.Cell];

    public override IEnumerable<ActionInfo> Actions(UnitData unit, IEnumerable<UnitAction> available)
    {
        List<ActionInfo> actions = [];
        Dictionary<UnitAction, IEnumerable<Vector2I>> targets = available.Where((a) => a.RequiresTarget).ToDictionary((a) => a, (a) => a.GetTargetCells(unit, unit.Cell).Where((c) => a.CanPerform(unit, unit.Cell, c)));

        actions.AddRange(available.Where((a) => !a.RequiresTarget && a.CanPerform(unit, unit.Cell)).Select((a) => new ActionInfo(a, [unit.Cell], GridData.InvalidCell, [unit.Cell])));

        if (AttackInRange)
        {
            foreach ((UnitAction action, IEnumerable<Vector2I> cells) in targets)
                foreach (Vector2I cell in cells)
                    if (!unit.Grid.Occupants[cell].Faction.AlliedTo(unit.Faction))
                        actions.Add(new(action, action.GetSourceCells(unit, cell), cell, [unit.Cell]));
        }
        if (SupportInRange)
        {
            foreach ((UnitAction action, IEnumerable<Vector2I> cells) in targets)
                foreach (Vector2I cell in cells)
                    if (unit.Grid.Occupants[cell].Faction.AlliedTo(unit.Faction))
                        actions.Add(new(action, action.GetSourceCells(unit, cell), cell, [unit.Cell]));
        }

        return actions;
    }

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> choices, IEnumerable<Vector2I> traversable) => unit.Cell;
}