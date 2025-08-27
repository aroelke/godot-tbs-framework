using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class SwitchBehavior : Behavior
{
    private bool _switched = false;

    [Export] public bool CanRevert = false;

    [Export] public bool MeetAllConditions = false;

    public Behavior Initial { get; private set; } = null;

    public Behavior Final { get; private set; } = null;

    public IEnumerable<SwitchCondition> Conditions { get; private set; } = [];

    public Behavior TargetBehavior()
    {
        if (CanRevert || !_switched)
            _switched = MeetAllConditions ? Conditions.All(static (c) => c.Satisfied) : Conditions.Any(static (c) => c.Satisfied);
        return _switched ? Final : Initial;
    }

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(IUnit unit, IGrid grid) => TargetBehavior()?.Actions(unit, grid) ?? [];

    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => TargetBehavior()?.Destinations(unit, grid) ?? [];

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        int behaviors = GetChildren().OfType<Behavior>().Count();
        if (behaviors < 2)
            warnings.Add("Not enough behaviors to switch between.");
        else if (behaviors > 2)
            warnings.Add("Too many behaviors to choose from. Only the first two will be used.");
        
        if (!GetChildren().OfType<SwitchCondition>().Any())
            warnings.Add("No switching condition has been defined. It won't be possible to switch behaviors.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        List<Behavior> behaviors = [.. GetChildren().OfType<Behavior>()];
        if (behaviors.Count > 0)
            Initial = behaviors[0];
        if (behaviors.Count > 1)
            Final = behaviors[1];

        Conditions = GetChildren().OfType<SwitchCondition>();
    }
}