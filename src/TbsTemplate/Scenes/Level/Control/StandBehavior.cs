using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>A behavior for a <see cref="Unit"/> that does not move.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : UnitBehavior
{
    /// <summary>Whether or not the unit should attack enemies in range.</summary>
    [Export] public bool AttackInRange = false;

    public override Vector2I DesiredMoveTarget(Unit unit) => unit.Cell;
    public override (StringName, GridNode) DesiredAction(Unit unit)
    {
        if (!AttackInRange)
            return ("End", unit);
        else
        {
            IEnumerable<Vector2I> attackable = unit.AttackableCells();
            IEnumerable<Unit> targets = unit.Grid.Occupants.Where((p) => attackable.Contains(p.Key)).Select((p) => p.Value).OfType<Unit>();
            if (targets.Any())
                return ("Attack", targets.First());
            else
                return ("End", unit);
        }
    }
}