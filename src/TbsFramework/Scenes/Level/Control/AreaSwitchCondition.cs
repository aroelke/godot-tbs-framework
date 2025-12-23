using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Object.Group;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Behavior switching condition that triggers based on a unit from a set of units entering an area of the map.</summary>
/// <remarks>
/// Note that this condition is not checked continuously; it only updates via the <see cref="Update"/> function, which
/// is automatically called every time a unit finishes its action.
/// </remarks>
public abstract partial class AreaSwitchCondition : SwitchCondition
{
    /// <summary>Set of units that can explicitly trigger the condition.</summary>
    [Export] public Unit[] TriggerUnits = [];

    /// <summary>Set of armies whose units can trigger the condition, even if they enter the map later.</summary>
    [Export] public Army[] TriggerArmies = [];

    /// <summary>
    /// <para><c>true</c>: Condition triggers if units are in the area.</para>
    /// <para><c>false</c>: Condition triggers if units are not in the area.</para>
    /// </summary>
    [Export] public bool Inside = true;

    /// <summary>Whether or not all trigger units have to be in or out of the area to satisfy the condition.</summary>
    [Export] public bool RequiresEveryone = false;

    /// <returns>The set of cells that defines the trigger region.</returns>
    public abstract HashSet<Vector2I> GetRegion();

    /// <returns>The set of all existing units that can trigger the condition.</returns>
    public IEnumerable<Unit> GetTriggerUnits()
    {
        List<Unit> applicable = [.. TriggerUnits];
        foreach (Army army in TriggerArmies)
            applicable.AddRange(army);
        return applicable;
    }

    /// <summary>
    /// When a unit finishes its action, check if the condition is satisfied and then update its <see cref="Satisfied"/>
    /// property accordingly.
    /// </summary>
    /// <param name="unit">Unit that finished moving.</param>
    public void Update(Unit unit)
    {
        if (!GetTriggerUnits().Any())
            return;

        HashSet<Vector2I> region = GetRegion();
        IEnumerable<Unit> applicable = GetTriggerUnits();
        Func<Func<Unit, bool>, bool> matcher = RequiresEveryone ? applicable.All : applicable.Any;
        Func<Unit, bool> container = Inside ? (u) => region.Contains(u.Cell) : (u) => !region.Contains(u.Cell);

        Satisfied = matcher(container);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (TriggerUnits.Length == 0 && TriggerArmies.Length == 0)
            warnings.Add("This condition doesn't apply to any units and will never be satisfied.");

        return [.. warnings];
    }


    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.ActionEnded += Update;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.ActionEnded -= Update;
    }
}