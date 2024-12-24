using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.AI;

/// <summary>Movement behavior that prevents a unit from moving.</summary>
[GlobalClass, Tool]
public partial class StandBehavior : MovementBehavior
{
    public override Vector2I Target(Unit unit) => unit.Cell;
}