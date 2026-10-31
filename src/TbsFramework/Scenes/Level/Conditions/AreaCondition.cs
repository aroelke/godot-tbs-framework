using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that indicates units are inside or outside an area on the map. How the area is specified is implementation-defined. </summary>
[GlobalClass, Tool]
public abstract partial class AreaCondition : GridCondition
{
    private bool _everyone = false;

    /// <summary>Units to test for presence in the area.</summary>
    [Export] public UnitFilter TriggerUnits = null;

    /// <summary>Whether the units should be inside or outside the area.</summary>
    [Export] public bool Inside = true;

    /// <summary>Whether all applicable units must be inside or outside the area to satisfy the condition or only some of them need to be.</summary>
    [Export] public bool RequiresEveryone
    {
        get => _everyone;
        set
        {
            if (value != _everyone)
            {
                _everyone = value;
                NotifyPropertyListChanged();
            }
        }
    }

    /// <summary>Inequality to use to test the number of units in the region against <see cref="Amount"/> if <see cref="RequiresEveryone"/> is <c>false</c>.</summary>
    [Export] public Inequality Inequality = Inequality.GreaterOrEqual;

    /// <summary>Value to test the number of units in the region if <see cref="RequiresEveryone"/> is <c>false</c>.</summary>
    [Export] public int Amount = 1;

    /// <returns>The set of cells on the <paramref name="grid"/> that define the area.</returns>
    public abstract IEnumerable<Vector2I> GetRegion(GridData grid);

    public override bool IsSatisfied(GridData grid)
    {
        IEnumerable<UnitData> units = (TriggerUnits is null ? grid.Occupants.Values : TriggerUnits.GetUnits(grid)).Where((u) => u.Health > 0);
        Func<UnitData, bool> matches = Inside ? (u) => GetRegion(grid).Contains(u.Cell) : (u) => !GetRegion(grid).Contains(u.Cell);
        return RequiresEveryone ? units.All(matches) : Inequality.Compare(units.Count(matches), Amount);
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (Engine.IsEditorHint() && (property["name"].AsStringName() == PropertyName.Inequality || property["name"].AsStringName() == PropertyName.Amount) && _everyone)
            property["usage"] = Variant.From(PropertyUsageFlags.NoEditor);
    }
}