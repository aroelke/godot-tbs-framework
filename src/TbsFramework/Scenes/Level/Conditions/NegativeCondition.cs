using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that represents the opposite of another condition.</summary>
[GlobalClass, Tool]
public partial class NegativeCondition : GridCondition
{
    /// <summary>Grid condition to negate.</summary>
    [Export] public GridCondition Condition = null;

    public override bool IsSatisfied(GridData grid) => !Condition.IsSatisfied(grid);
}