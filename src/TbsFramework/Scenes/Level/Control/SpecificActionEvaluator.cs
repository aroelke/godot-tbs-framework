using System.Linq;
using Godot;

namespace TbsFramework.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class SpecificActionEvaluator : ActionEvaluator
{
    [Export] public UnitAction[] TrackedActions = [];

    // No need to check if the unit can perform the action because it already did
    public override double Evaluate(SimulatedAction action) => TrackedActions.Contains(action.Action) ? 1 : 0;
}