using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Controller for determining which units act in a turn and how they act.</summary>
public abstract partial class ArmyController : Node
{
    /// <summary>Signals that a selection has been canceled.</summary>
    [Signal] public delegate void SelectionCanceledEventHandler();

    /// <summary>Signals that a <see cref="Unit"/> has been chosen to act.</summary>
    /// <param name="unit">Selected unit.</param>
    [Signal] public delegate void UnitSelectedEventHandler(Unit unit);

    /// <summary>Signals that a unit's action is being skipped.</summary>
    [Signal] public delegate void TurnSkippedEventHandler();

    /// <summary>Signals that a change has been made to the path during path selection.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="path">New path after update.</param>
    [Signal] public delegate void PathUpdatedEventHandler(Unit unit, Godot.Collections.Array<Vector2I> path);

    /// <summary>Signals that a path for a <see cref="Unit"/> to move on has been chosen.</summary>
    /// <param name="unit">Unit that will move along the path.</param>
    /// <param name="path">Contiguous list of cells for the unit to move through.</param>
    [Signal] public delegate void PathConfirmedEventHandler(Unit unit, Godot.Collections.Array<Vector2I> path);
    /// <summary>Signals that an action has been chosen for a unit.</summary>
    /// <param name="unit">Unit being commanded.</param>
    /// <param name="command">String representing the action to perform.</param>
    [Signal] public delegate void UnitCommandedEventHandler(Unit unit, StringName command);

    /// <summary>Signals that a target for an action has been chosen.</summary>
    /// <param name="source">Unit performing the action.</param>
    /// <param name="target">Target of the action.</param>
    [Signal] public delegate void TargetChosenEventHandler(Unit source, Unit target);

    private Army _army = null;

    /// <summary>Army being controlled. Should be the direct parent of this controller.</summary>
    public Army Army => _army ??= GetParentOrNull<Army>();

    /// <summary>Cursor used for indicating or making a selection.</summary>
    [Export] public Cursor Cursor = null;

    /// <summary>Grid that the army's units will be acting on.</summary>
    [Export] public abstract Grid Grid { get; set; }

    /// <summary>Perform any setup needed to begin the army's turn.</summary>
    public abstract void InitializeTurn();

    /// <summary>Choose a unit in the army to select. Once the <see cref="Unit"/> has been selected, emit <c>UnitSelected</c>.</summary>
    public abstract void SelectUnit();

    /// <summary>Choose the path along which a unit will move. Once the path has been determined, emit <c>UnitMoved</c>.</summary>
    /// <param name="unit">Unit to move.</param>
    public abstract void MoveUnit(Unit unit);

    /// <summary>Choose an action for a unit to perform. Once a command has been selected, emit <c>UnitCommanded</c>.</summary>
    /// <param name="source">Unit chosen to perform a command.</param>
    /// <param name="commands">List of commands available to perform.</param>
    /// <param name="cancel">Command to perform on cancel.</param>
    public abstract void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel);

    /// <summary>Choose the target for an action that was selected.</summary>
    /// <param name="source">Unit that will perform the action.</param>
    /// <param name="targets">Cells <paramref name="source"/> can act on.</param>
    public abstract void SelectTarget(Unit source, IEnumerable<Vector2I> targets);

    /// <summary>Clean up at the end of a unit's action and get ready for the next unit's action.</summary>
    public abstract void FinalizeAction();

    /// <summary>Clean up at the end of an army's turn.</summary>
    public abstract void FinalizeTurn();

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetParentOrNull<Army>() is null)
            warnings.Add("This controller does not belong to an army. It has nothing to control.");

        return [.. warnings];
    }
}