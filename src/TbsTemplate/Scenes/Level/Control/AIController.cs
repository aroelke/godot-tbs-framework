using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : ArmyController
{
    public enum DecisionType
    {
        ClosestEnemy
    }

    private Unit _selected = null;
    private Vector2I _destination = -Vector2I.One;
    private StringName _action = null;
    private Unit _target = null;

    [Export] public DecisionType Decision = DecisionType.ClosestEnemy;

    public override void InitializeTurn()
    {
        Cursor.Halt(hide:true);

        _selected = null;
        _destination = -Vector2I.One;
        _action = null;
        _target = null;
    }

    public override async void SelectUnit()
    {
        // Compute this outside the task because it calls Node.GetChildren(), which has to be called on the same thread as that node.
        // Also, use a collection expression to immediately evaluated it rather than waiting until later, because that will be in the
        // wrong thread.
        IEnumerable<Unit> available = [.. ((IEnumerable<Unit>)Army).Where(static (u) => u.Active)];
        IEnumerable<Unit> enemies = Cursor.Grid.Occupants.Values.OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u));

        (_selected, _destination, _action, _target) = await Task.Run<(Unit, Vector2I, StringName, Unit)>(() => {
            Unit selected = null;
            Vector2I destination = -Vector2I.One;
            StringName action = null;
            Unit target = null;

            switch (Decision)
            {
            case DecisionType.ClosestEnemy:
                selected = available.MinBy((u) => enemies.Select((e) => u.Cell.DistanceTo(e.Cell)).Min());

                IEnumerable<Vector2I> destinations = selected.Behavior.Destinations(selected);
                Dictionary<StringName, IEnumerable<Vector2I>> actions = selected.Behavior.Actions(selected);

                IEnumerable<Unit> attackable = actions.ContainsKey("Attack") ? actions["Attack"].Select((c) => selected.Grid.Occupants[c]).OfType<Unit>().OrderBy((u) => u.Cell.DistanceTo(selected.Cell)) : [];
                if (attackable.Any())
                {
                    action = "Attack";
                    target = attackable.First();
                    destination = selected.AttackableCells(target.Cell).Where(destinations.Contains).OrderBy((c) => selected.Behavior.GetPath(selected, c).Cost).First();
                }
                else
                {
                    IEnumerable<Unit> enemies = selected.Grid.Occupants.Select(static (p) => p.Value).OfType<Unit>().Where((u) => !u.Army.Faction.AlliedTo(selected)).OrderBy((u) => u.Cell.DistanceTo(selected.Cell));
                    if (enemies.Any())
                        destination = destinations.OrderBy((c) => c.DistanceTo(enemies.First().Cell)).OrderBy((c) => selected.Behavior.GetPath(selected, c).Cost).First();
                    else
                        destination = selected.Cell;
                    action = "End";
                }
                break;
            default:
                break;
            }

            return (selected, destination, action, target);
        });

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