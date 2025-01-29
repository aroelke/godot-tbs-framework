using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>A behavior for a <see cref="Unit"/> that does not move.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : UnitBehavior
{
    public override Vector2I DesiredMoveTarget(Unit unit) => unit.Cell;
    public override (StringName, GridNode) DesiredAction(Unit unit) => ("End", unit);
}