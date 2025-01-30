using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;

    public override void InitializeTurn()
    {
        Cursor.Halt(hide:true);

        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    public override void SelectUnit()
    {
        _selected = ((IEnumerable<Unit>)Army).Where((u) => u.Active).First();

        IEnumerable<Vector2I> destinations = _selected.Behavior.Destinations(_selected);
        Dictionary<StringName, IEnumerable<Vector2I>> actions = _selected.Behavior.Actions(_selected);
        
        IEnumerable<Unit> attackable = actions.ContainsKey("Attack") ? actions["Attack"].Select((c) => _selected.Grid.Occupants[c]).OfType<Unit>().OrderBy((u) => u.Cell.DistanceTo(_selected.Cell)) : [];
        if (attackable.Any())
        {
            _action = "Attack";
            _target = attackable.First();
            _destination = _selected.AttackableCells(_target.Cell).Where(destinations.Contains).OrderBy((c) => Path.Empty(_selected.Grid, destinations).Add(_selected.Cell).Add(c).Cost).First();
        }
        else
        {
            IEnumerable<Unit> enemies = _selected.Grid.Occupants.Select(static (p) => p.Value).OfType<Unit>().Where((u) => !u.Army.Faction.AlliedTo(_selected)).OrderBy((u) => u.Cell.DistanceTo(_selected.Cell));
            if (enemies.Any())
                _destination = destinations.OrderBy((c) => c.DistanceTo(enemies.First().Cell)).First();
            else
                _destination = _selected.Cell;
            _action = "End";
        }

        EmitSignal(SignalName.UnitSelected, _selected);
    }

    public override void MoveUnit(Unit unit)
    {
        EmitSignal(SignalName.PathConfirmed, unit, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, _destination)));
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        EmitSignal(SignalName.UnitCommanded, source, _action);
    }

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Name}'s target has not been determined");
        if (!targets.Contains(_target.Cell))
            throw new InvalidOperationException($"{source.Name} can't target {_target}");
        EmitSignal(SignalName.TargetChosen, source, _target);
    }

    public override void FinalizeAction() {}

    // Don't resume the cursor.  The player controller will be responsible for that.
    public override void FinalizeTurn() {}
}