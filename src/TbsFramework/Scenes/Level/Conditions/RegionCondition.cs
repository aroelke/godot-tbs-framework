using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Grid condition that indicates whether or not units are inside or outside a specially-defined region defined using a <see cref="TileMapLayer"/>.
[GlobalClass, Tool]
public partial class RegionCondition : AreaCondition
{
    /// <summary>
    /// Name of the <see cref="TileMapLayer"/> that defines the region. This will be used to get the region's cells from the <see cref="GridData"/> passed
    /// into <see cref="GetRegion"/>.
    /// </summary>
    [Export] public StringName RegionName = "";

    public override IEnumerable<Vector2I> GetRegion(GridData grid) => grid.Regions.GetValueOrDefault(RegionName, []);

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (Engine.IsEditorHint() && property["name"].AsStringName() == PropertyName.RegionName)
            HighlightedRegion.AddRegionsToProperty(property);
    }
}