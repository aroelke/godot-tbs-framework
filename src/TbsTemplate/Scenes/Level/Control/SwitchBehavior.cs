using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>
/// <see cref="Unit"/> behavior that can switch between two other behaviors based on a <see cref="SwitchCondition"/>. Can be configured to only
/// switch once, even if the condition becomes unsatisfied later, or switch back and forth based on the condition's satisfaction.
/// </summary>
[Tool]
public partial class SwitchBehavior : Behavior
{
    private bool _switched = false;

    /// <summary>Whether or not the behavior can revert back to <see cref="Initial"/> after switching to <see cref="Final"/>.</summary>
    [Export] public bool CanRevert = false;

    /// <summary>
    /// If there are multiple <see cref="SwitchCondition"/> children of this node, <c>true</c> means a behavior switch occurs only if all of
    /// those conditions are satisfied, and <c>false</c> means a switch occurs if any of them are satisfied.
    /// </summary>
    [Export] public bool MeetAllConditions = false;

    /// <summary>Behavior to use before the <see cref="SwitchCondition"/> is satisfied.</summary>
    public Behavior Initial { get; private set; } = null;

    /// <summary>Behavior to use after the <see cref="SwitchCondition"/> is satisfied.</summary>
    public Behavior Final { get; private set; } = null;

    /// <summary>Conditions used to determine if the behavior should switch.</summary>
    public IEnumerable<SwitchCondition> Conditions { get; private set; } = [];

    /// <summary>Compute whether a behavior switch should occur.</summary>
    /// <returns>
    /// <see cref="Initial"/>, if a behavior switch has not occurred (or if it reverted), and <see cref="Final"/> if a switch
    /// has occurred.
    /// </returns>
    public Behavior TargetBehavior()
    {
        if (CanRevert || !_switched)
            _switched = MeetAllConditions ? Conditions.All(static (c) => c.Satisfied) : Conditions.Any(static (c) => c.Satisfied);
        return _switched ? Final : Initial;
    }

    public override Dictionary<StringName, IEnumerable<Vector2I>> Actions(IUnit unit, IGrid grid) => TargetBehavior()?.Actions(unit, grid) ?? [];

    public override IEnumerable<Vector2I> Destinations(IUnit unit, IGrid grid) => TargetBehavior()?.Destinations(unit, grid) ?? [];

    /// <summary>Reset the state of the behavior. Mainly intended to be used for testing.</summary>
    public void Reset() => _switched = false;

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