using Godot;

namespace TbsFramework.Scenes.Level.Control;

[GlobalClass, Tool]
public partial class PathEvaluator : ActionEvaluator
{
    [Export] public bool PreferShorter = true;

    public override double Evaluate(SimulatedAction action)
    {
        double value = ((double)action.Traversed.Count)/action.GetActor().Stats.MoveDistance;
        if (PreferShorter)
            return 1 - value;
        else
            return value;
    }
}