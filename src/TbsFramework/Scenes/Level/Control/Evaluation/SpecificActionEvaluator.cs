using System.Linq;
using Godot;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Evaluates actions based on if they are part of a desired set.</summary>
[GlobalClass, Tool]
public partial class SpecificActionEvaluator : ActionEvaluator
{
    /// <summary>Set of actions. If a simulated action is in this set, it is evaluated high. Otherwise, it is evaluated low.</summary>
    [Export] public UnitAction[] TrackedActions = [];

    // No need to check if the unit can perform the action because it already did
    public override double Evaluate(SimulatedAction action) => TrackedActions.Contains(action.Action) ? 1 : 0;
}