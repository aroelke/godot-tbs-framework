using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Conditions;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>
/// Behavior that switches between two sub-Behaviors based on one or more <see cref="GridCondition"/>s.
/// </summary>
[GlobalClass, Tool]
public partial class ConditionalBehavior() : Behavior
{
    /// <summary>Behavior to use before the condition is triggered.</summary>
    [Export] public Behavior InitialBehavior = null;

    /// <summary>Behavior to use after the condition is triggered.</summary>
    [Export] public Behavior FinalBehavior = null;

    /// <summary>Condition that triggers the switch.</summary>
    [Export] public GridCondition Condition = null;

    /// <summary>
    /// Whether or not this behavior can switch back to <see cref="InitialBehavior"/> if the conditions stop being satisfied. If <c>false</c>, <see cref="FinalBehavior"/>
    /// will continue to be used even if the conditions stop being met.
    /// </summary>
    [Export] public bool CanRevert = false;

    /// <param name="grid">Grid to look at to determine if the switching conditions are satisfied.</param>
    /// <returns>
    /// <c>true</c> if the conditions are satisfied for <paramref name="grid"/> to switch behaviors or if this behavior can't revert and has already switched for
    /// <paramref name="grid"/>, and <c>false</c> otherwise.
    /// </returns>
    public bool HasSwitched(GridData grid)
    {
        if (CanRevert)
            return Condition.IsSatisfied(grid);
        else if (grid.SwitchedBehaviors.Contains(this))
            return true;
        else if (Condition.IsSatisfied(grid))
        {
            grid.SwitchedBehaviors.Add(this);
            return true;
        }
        else
            return false;
    }

    /// <param name="grid">Grid to look at to determine if the switching conditions are satisfied.</param>
    /// <returns>
    /// <see cref="FinalBehavior"/> if <see cref="HasSwitched(GridData)"/> is <c>true</c> for <paramref name="grid"/> and <see cref="InitialBehavior"/>
    /// if it doesn't.
    /// </returns>
    public Behavior GetCurrentBehavior(GridData grid) => HasSwitched(grid) ? FinalBehavior : InitialBehavior;

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable) => GetCurrentBehavior(unit.Grid).ChooseDestination(unit, destinations, traversable);
    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable) => GetCurrentBehavior(unit.Grid).GetActions(unit, available, traversable);
}