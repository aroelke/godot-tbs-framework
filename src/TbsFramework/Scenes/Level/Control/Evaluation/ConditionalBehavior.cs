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
    private bool _switched = false;

    /// <summary>Behavior to use before the condition is triggered.</summary>
    [Export] public Behavior InitialBehavior = null;

    /// <summary>Behavior to use after the condition is triggered.</summary>
    [Export] public Behavior FinalBehavior = null;

    /// <summary>Conditions that can trigger the switch.</summary>
    [Export] public GridCondition[] Conditions = [];

    /// <summary>Whether to trigger a swtich if all elements of <see cref="Conditions"/> are satisfied (<c>true</c>) or any of them are satisfied (<c>false</c>).</summary>
    [Export] public bool MeetAllConditions = false;

    /// <summary>
    /// Whether or not this behavior can switch back to <see cref="InitialBehavior"/> if the conditions stop being satisfied. If <c>false</c>, <see cref="FinalBehavior"/>
    /// will continue to be used even if the conditions stop being met.
    /// </summary>
    [Export] public bool CanRevert = false;

    /// <summary>Whether or not the behavior is currently switched.</summary>
    public bool Switched
    {
        get
        {
            bool triggered() => MeetAllConditions ? Conditions.All((c) => c.Satisfied) : Conditions.Any((c) => c.Satisfied);

            if (CanRevert)
                return triggered();
            else
                return _switched || (_switched = triggered());
        }
    }

    /// <summary>Before switching, points to <see cref="InitialBehavior"/>. Afterwards, points to <see cref="FinalBehavior"/>.</summary>
    public Behavior CurrentBehavior => Switched ? FinalBehavior : InitialBehavior;

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable) => CurrentBehavior.ChooseDestination(unit, destinations, traversable);
    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable) => CurrentBehavior.GetActions(unit, available, traversable);
    public override void Reset() => _switched = false;
}