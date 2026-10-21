using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>
/// <para>Grid condition that tests if the number of units in a set satisfies an inequality.</para>
/// 
/// <para>To implement a condition that checks for a boss defeat, set <see cref="AreaCondition.TriggerUnits">TriggerUnits</see> to track the desired unit,
/// <see cref="AreaCondition.Inequality">Inequality</see> to <see cref="Inequality.Less"/>, and <see cref="AreaCondition.Amount">Amount</see> to 1.</para>
/// 
/// <para>To implement a condition that checks for an entire army to be defeated, set <see cref="AreaCondition.TriggerUnits">TriggerUnits</see>  to track the
/// desired <see cref="Faction"/>, <see cref="Inequality"/> to <see cref="Inequality.Less"/>, and <see cref="AreaCondition.Amount">Amount</see>
/// to 1.</para>
/// </summary>
[GlobalClass, Tool]
public partial class UnitsRemainingCondition : AreaCondition
{
    public override IEnumerable<Vector2I> GetRegion(GridData grid) => grid.AllCells;
}