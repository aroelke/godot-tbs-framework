using Godot;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>Evaluates an action based on the length of the path the actor took to get to its destination.</summary>
[GlobalClass, Tool]
public partial class PathEvaluator : ActionEvaluator
{
    /// <summary><c>true</c> if shorter paths should be evaluated higher and <c>false</c> if longer paths should be higher.</summary>
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