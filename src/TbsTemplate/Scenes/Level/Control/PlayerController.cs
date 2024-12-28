using Godot;
using System;
using TbsTemplate.Scenes.Level.Control;
using TbsTemplate.Scenes.Level.Object;

[SceneTree]
public partial class PlayerController : ArmyController
{
    private void ConfirmCursorSelection(Vector2I cell)
    {
        GD.Print($"Select cell {cell}");

        if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit unit && unit.Faction == Army.Faction)
        {
            EmitSignal(SignalName.UnitSelected, unit);
            Cursor.CellSelected -= ConfirmCursorSelection;
        }
    }

    public override void InitializeTurn()
    {
        Cursor.Resume();
    }

    public override void SelectUnit()
    {
        Cursor.CellSelected += ConfirmCursorSelection;
    }

    public override void MoveUnit(Unit unit)
    {
        throw new NotImplementedException();
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands)
    {
        throw new NotImplementedException();
    }

    public override void FinalizeTurn()
    {
        throw new NotImplementedException();
    }
}
