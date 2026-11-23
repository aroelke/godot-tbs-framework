using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that is satisfied based on the satisfaction of one or more other conditions.</summary>
[GlobalClass, Tool]
public partial class AggregateCondition : GridCondition
{
    /// <summary>Conditions to combine to determine satisfaction.</summary>
    [Export] public GridCondition[] Children = [];

    /// <summary>Whether all conditions must be satisfied for this one to be satisfied (<c>true</c>) or any of them (<c>false</c>).</summary>
    [Export] public bool RequiresAll = false;

    public override bool IsSatisfied(GridData grid)
    {
        bool test(GridCondition condition) => condition.IsSatisfied(grid);
        return RequiresAll ? Children.All(test) : Children.Any(test);
    }
}