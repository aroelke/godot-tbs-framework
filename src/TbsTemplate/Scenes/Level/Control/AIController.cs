using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    private Unit _target = null;

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
        EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, unit.Behavior.DesiredMoveTarget(unit))));
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        (StringName action, GridNode target) = source.Behavior.DesiredAction(source);
        _target = target as Unit;
        EmitSignal(SignalName.UnitCommanded, source, action);
    }

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Name}'s target has not been determined");
        EmitSignal(SignalName.TargetChosen, source, _target);
    }

    public override void FinalizeAction() {}

    // Don't resume the cursor.  The player controller will be responsible for that.
    public override void FinalizeTurn() {}
}