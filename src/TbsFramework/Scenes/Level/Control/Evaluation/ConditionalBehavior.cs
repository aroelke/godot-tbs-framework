using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Conditions;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

public partial class SwitchBehavior() : Behavior
{
    private bool _switched = false;

    [Export] public Behavior InitialBehavior = null;
    [Export] public Behavior FinalBehavior = null;
    [Export] public GridCondition[] Conditions = null;
    [Export] public bool MeetAllConditions = false;
    [Export] public bool CanRevert = false;

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

    public Behavior CurrentBehavior => Switched ? FinalBehavior : InitialBehavior;

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable) => CurrentBehavior.ChooseDestination(unit, destinations, traversable);
    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable) => GetActions(unit, available, traversable);
    public override void Reset() => _switched = false;
}