using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

public partial class SwitchBehavior() : Behavior
{
    private bool _switched = false;

    [Export] public Behavior InitialBehavior = null;
    [Export] public Behavior FinalBehavior = null;
    [Export] public SwitchCondition[] Conditions = null;
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

    private SwitchBehavior(Behavior initial, Behavior final, SwitchCondition[] conditions, bool all, bool revertable, bool switched) : this()
    {
        InitialBehavior = initial.Clone();
        FinalBehavior = final.Clone();
        Conditions = [.. conditions.Select((c) => c.Clone())];
        MeetAllConditions = all;
        CanRevert = revertable;
        _switched = switched;
    }

    private SwitchBehavior(SwitchBehavior original) : this(original.InitialBehavior, original.FinalBehavior, original.Conditions, original.MeetAllConditions, original.CanRevert, original._switched) {}

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable) => CurrentBehavior.ChooseDestination(unit, destinations, traversable);
    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable) => GetActions(unit, available, traversable);

    public override Behavior Clone() => new SwitchBehavior(this);
}