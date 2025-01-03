using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    public override void InitializeTurn()
    {
        Cursor.Halt(hide:true);
    }

    public override void SelectUnit()
    {
        EmitSignal(SignalName.UnitSelected, ((IEnumerable<Unit>)Army).Where((u) => u.Active).First());
    }

    public override void MoveUnit(Unit unit)
    {
        Godot.Collections.Array<Vector2I> path = [unit.Cell];
        EmitSignal(SignalName.PathConfirmed, path);
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        EmitSignal(SignalName.UnitCommanded, new StringName("End"));
    }

    public override void SelectTarget(Unit source)
    {
        throw new System.NotImplementedException();
    }

    // Don't resume the cursor.  The player controller will be responsible for that.
    public override void FinalizeTurn() {}
}