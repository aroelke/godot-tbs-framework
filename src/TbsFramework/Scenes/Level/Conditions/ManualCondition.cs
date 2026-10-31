using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>
/// Represents a grid condition that can be manually toggled to become satisfied or not (basically it's
/// just a boolean variable and is mainly useful for testing).
/// </summary>
/// <remarks>
/// Be careful when switching this condition, as it doesn't get copied during AI evaluation and doesn't
/// automatically revert if something is undone.
/// </remarks>
[GlobalClass, Tool]
public partial class ManualCondition : GridCondition
{
    /// <summary>Whether or not this condition is satisfied.</summary>
    [Export] public bool Satisfied = false;

    public override bool IsSatisfied(GridData grid) => Satisfied;
}