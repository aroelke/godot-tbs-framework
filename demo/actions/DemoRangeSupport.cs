using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;

namespace TbsFramework.Demo;

[GlobalClass, Tool]
public partial class DemoRangeSupport : ActionRange
{
    public override bool InRange(UnitData unit, Vector2I source, Vector2I target) => unit.Stats.SupportRange.Contains(source.ManhattanDistanceTo(target));

    public override IEnumerable<Vector2I> GetAllCellsInRange(UnitData unit, Vector2I cell) => unit.GetSupportableCells(cell);

    public override IEnumerable<Vector2I> GetValidCellsInRange(UnitData unit, Vector2I cell) =>
        GetAllCellsInRange(unit, cell).Where((c) => unit.Grid.Occupants.TryGetValue(c, out UnitData occupant) && unit.Faction.AlliedTo(occupant.Faction));
}