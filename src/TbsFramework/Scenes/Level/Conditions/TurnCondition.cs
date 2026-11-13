using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>
/// Represents a condition based on the current turn. Becomes satisfied once the turn reaches the indicated value and normally can't become
/// unsatisfied again.
/// </summary>
[GlobalClass, Tool]
public partial class TurnCondition : GridCondition
{
    /// <summary>Turn to trigger the condition.</summary>
    [Export] public int Turn = 1;

    public override bool IsSatisfied(GridData grid) => grid.Turn >= Turn;
}