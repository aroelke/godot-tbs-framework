using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.AI;

/// <summary>Automatically controls units based on their <see cref="UnitBehavior"/>s and the state of the level.</summary>
public partial class AIController : Node
{
    /// <summary>Signals that a <see cref="Unit"/> has been chosen to act.</summary>
    /// <param name="unit">Selected unit.</param>
    [Signal] public delegate void UnitSelectedEventHandler(Unit unit);

    [Signal] public delegate void UnitMovedEventHandler(Godot.Collections.Array<Vector2I> path);

    private Army _army = null;
    private Army Army => _army ??= GetParentOrNull<Army>();

    /// <summary>Choose a unit in the army to select and signal that it has been selected.</summary>
    public void SelectUnit()
    {
        EmitSignal(SignalName.UnitSelected, ((IEnumerable<Unit>)Army).First());
    }

    /// <summary>Choose the path along which a unit will move.</summary>
    /// <param name="unit">Unit to move.</param>
    public void MoveUnit(Unit unit)
    {
        Godot.Collections.Array<Vector2I> path = [unit.Cell];
        EmitSignal(SignalName.UnitMoved, path);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetParentOrNull<Army>() is null)
            warnings.Add("This controller does not belong to an army. It has nothing to control.");

        return [.. warnings];
    }
}