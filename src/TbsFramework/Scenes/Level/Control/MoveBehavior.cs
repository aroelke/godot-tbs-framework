using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Scenes.Level.Control;

/// <summary><see cref="Unit"/> behavior that allows the unit to move around the grid to perform actions.</summary>
[Tool]
public partial class MoveBehavior : Behavior
{
    public override IEnumerable<Vector2I> Destinations(UnitData unit) => unit.GetTraversableCells().Where((c) => !unit.Grid.Occupants.ContainsKey(c) || c == unit.Cell);

    public override IEnumerable<ActionInfo> Actions(UnitData unit, IEnumerable<UnitAction> available)
    {
        IEnumerable<Vector2I> destinations = Destinations(unit);
        return available.SelectMany((a) => {
            if (a.RequiresTarget)
                return a.GetValidTargetCells(unit, destinations).Select((c) => new ActionInfo(a, a.GetSourceCells(unit, c), c, destinations));
            else
            {
                IEnumerable<Vector2I> allowed = destinations.Where((c) => a.CanPerform(unit, c));
                return allowed.Any() ? [new ActionInfo(a, allowed, GridData.InvalidCell, destinations)] : [];
            }
        });
    }
}