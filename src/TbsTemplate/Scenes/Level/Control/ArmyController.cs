using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Controller for determining which units act in a turn and how they act.</summary>
public abstract partial class ArmyController : Node
{
    private Army _army = null;

    /// <summary>Army being controlled. Should be the direct parent of this controller.</summary>
    public Army Army => _army ??= GetParentOrNull<Army>();

    /// <summary>Cursor used for indicating or making a selection.</summary>
    public Cursor Cursor = null;

    public abstract void InitializeTurn();

    /// <summary>Choose a unit in the army to select. Once the <see cref="Unit"/> has been selected, emit <c>UnitSelected</c>.</summary>
    public abstract void SelectUnit();

    /// <summary>Choose the path along which a unit will move. Once the path has been determined, emit <c>UnitMoved</c>.</summary>
    /// <param name="unit">Unit to move.</param>
    public abstract void MoveUnit(Unit unit);

    /// <summary>Choose an action for a unit to perform. Once a command has been selected, emit <c>UnitCommanded</c>.</summary>
    /// <param name="unit">Unit chosen to perform a command.</param>
    /// <param name="commands">List of commands available to perform.</param>
    /// <param name="cancel">Command to perform on cancel.</param>
    public abstract void CommandUnit(Unit unit, Godot.Collections.Array<StringName> commands, StringName cancel);

    /// <summary>Choose the target for an action that was selected.</summary>
    /// <param name="source">Unit that will perform the action.</param>
    public abstract void SelectTarget(Unit source);

    public abstract void FinalizeTurn();

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetParentOrNull<Army>() is null)
            warnings.Add("This controller does not belong to an army. It has nothing to control.");

        return [.. warnings];
    }
}