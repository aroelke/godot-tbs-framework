using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Control;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

[SceneTree]
public partial class PlayerController : ArmyController
{
    private Unit _selected = null, _target = null;
    IEnumerable<Vector2I> _traversable = null, _attackable = null, _supportable = null;
    private Path _path;

    private void ConfirmCursorSelection(Vector2I cell)
    {
        if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit unit && unit.Faction == Army.Faction)
        {
            EmitSignal(SignalName.UnitSelected, unit);
            Cursor.CellSelected -= ConfirmCursorSelection;
        }
    }

    private void AddToPath(Vector2I cell)
    {
        void UpdatePath(Path path) => EmitSignal(SignalName.PathUpdated, new Godot.Collections.Array<Vector2I>(_path = path));

        // If the previous cell was an ally that could be supported and moved through, add it to the path as if it
        // had been added in the previous movement
        if (_target is not null && _supportable.Contains(_target.Cell) && _traversable.Contains(_target.Cell))
            UpdatePath(_path.Add(_target.Cell));

        _target = null;
//        _command = null;

        IEnumerable<Vector2I> sources = [];
        if (Cursor.Grid.Occupants.GetValueOrDefault(cell) is Unit target)
        {
            // Compute cells the highlighted unit could be targeted from (friend or foe)
            if (target != _selected && Army.Faction.AlliedTo(target) && _supportable.Contains(cell))
                sources = _selected.SupportableCells(cell).Where(_traversable.Contains);
            else if (!Army.Faction.AlliedTo(target) && _attackable.Contains(cell))
                sources = _selected.AttackableCells(cell).Where(_traversable.Contains);
            sources = sources.Where((c) => !Cursor.Grid.Occupants.ContainsKey(c) || Cursor.Grid.Occupants[c] == _selected);

            if (sources.Any())
            {
                _target = target;

                // Store the action command related to selecting the target as if it were the command state
/*
                if (_attackable.Contains(cell))
                    _command = AttackLayer;
                else if (_supportable.Contains(cell))
                    _command = SupportLayer;
*/

                // If the end of the path isn't a cell that could act on the target, find the furthest one that can and add
                // it to the path
                if (!sources.Contains(_path[^1]))
                {
                    UpdatePath(sources.Select((c) => _path.Add(c).Clamp(_selected.Stats.Move)).OrderBy(
                        (p) => new Vector2I(-(int)p[^1].DistanceTo(cell), (int)p[^1].DistanceTo(_path[^1])),
                        static (a, b) => a < b ? -1 : a > b ? 1 : 0
                    ).First());
                }
            }
        }
        if (!sources.Any() && _traversable.Contains(cell))
            UpdatePath(_path.Add(cell).Clamp(_selected.Stats.Move));
    }

    private void ConfirmPathSelection(Vector2I cell)
    {
        if (!Cursor.Grid.Occupants.ContainsKey(cell))
        {
            EmitSignal(SignalName.UnitMoved, new Godot.Collections.Array<Vector2I>(_path));
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
        _target = null;
        _selected = unit;
        (_traversable, _attackable, _supportable) = unit.ActionRanges();
        _path = Path.Empty(Cursor.Grid, _traversable).Add(_selected.Cell);
        Cursor.CellChanged += AddToPath;
        Cursor.CellSelected += ConfirmPathSelection;
    }

    public override void EndMove()
    {
        Cursor.CellChanged -= AddToPath;
        Cursor.CellSelected -= ConfirmPathSelection;
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
