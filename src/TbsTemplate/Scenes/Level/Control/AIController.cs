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
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitSelected, ((IEnumerable<Unit>)Army).Where((u) => u.Active).First());
    }

    public override void MoveUnit(Unit unit)
    {
        Godot.Collections.Array<Vector2I> path = [unit.Cell];
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitMoved, unit, path);
    }

    public override void CommandUnit(Unit unit, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitCommanded, unit, new StringName("End"));
    }

    public override void SelectTarget(Unit source)
    {
        throw new System.NotImplementedException();
    }

    // Don't resume the cursor.  The player controller will be responsible for that.
    public override void FinalizeTurn() {}
}