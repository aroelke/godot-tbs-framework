using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

public partial class SwitchBehavior() : Behavior
{
    [Export] public Behavior InitialBehavior = null;
    [Export] public Behavior FinalBehavior = null;
    [Export] public SwitchCondition[] Conditions = null;
    [Export] public bool MeetAllConditions = false;

    public Behavior CurrentBehavior => (MeetAllConditions ? Conditions.All((c) => c.Satisfied) : Conditions.Any((c) => c.Satisfied)) ? FinalBehavior : InitialBehavior;

    private SwitchBehavior(Behavior initial, Behavior final, SwitchCondition[] conditions, bool all) : this()
    {
        InitialBehavior = initial.Clone();
        FinalBehavior = final.Clone();
        Conditions = [.. conditions.Select((c) => c.Clone())];
        MeetAllConditions = all;
    }

    private SwitchBehavior(SwitchBehavior original) : this(original.InitialBehavior, original.FinalBehavior, original.Conditions, original.MeetAllConditions) {}

    public override Vector2I ChooseDestination(UnitData unit, IEnumerable<Vector2I> destinations, IEnumerable<Vector2I> traversable) => CurrentBehavior.ChooseDestination(unit, destinations, traversable);
    public override IEnumerable<PerformableAction> GetActions(UnitData unit, IEnumerable<UnitAction> available, IEnumerable<Vector2I> traversable) => GetActions(unit, available, traversable);

    public override Behavior Clone() => new SwitchBehavior(this);
}