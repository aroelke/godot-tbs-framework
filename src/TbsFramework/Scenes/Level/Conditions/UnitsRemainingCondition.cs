using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>
/// Grid condition that tests if the number of units in a set satisfies an inequality.
/// 
/// To implement a condition that checks for a boss defeat, set <see cref="Units"/> to track the desired unit,
/// <see cref="Inequality"/> to <see cref="Inequality.Less"/>, and <see cref="Amount"/> to 1.
/// 
/// To implement a condition that checks for an entire army to be defeated, set <see cref="Units"/>  to track the
/// desired <see cref="Faction"/>, <see cref="Inequality"/> to <see cref="Inequality.Less"/>, and <see cref="Amount"/>
/// to 1.
/// </summary>
[GlobalClass, Tool]
public partial class UnitsRemainingCondition : GridCondition
{
    /// <summary>Set of units to test.</summary>
    [Export] public UnitFilter Units = null;

    /// <summary>Inequality to use to test the number of elements of <see cref="Units"/>.</summary>
    [Export] public Inequality Inequality = Inequality.Less;

    /// <summary>Value to test the number of elements of <see cref="Units"/> against.</summary>
    [Export] public int Amount = 1;

    public override bool IsSatisfied(GridData grid) => Units is not null && Inequality.Compare(Units.GetUnits(grid).Count((u) => u.Health > 0), Amount);
}