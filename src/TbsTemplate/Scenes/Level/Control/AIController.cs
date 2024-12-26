using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    public override void SelectUnit()
    {
        EmitSignal(SignalName.UnitSelected, ((IEnumerable<Unit>)Army).Where((u) => u.Active).First());
    }

    public override void MoveUnit(Unit unit)
    {
        Godot.Collections.Array<Vector2I> path = [unit.Cell];
        EmitSignal(SignalName.UnitMoved, path);
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands)
    {
        EmitSignal(SignalName.UnitCommanded, new StringName("End"), (Unit)null);
    }
}