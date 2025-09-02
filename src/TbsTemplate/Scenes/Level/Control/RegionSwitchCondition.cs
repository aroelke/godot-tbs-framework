using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class RegionSwitchCondition : AreaSwitchCondition
{
    [Export] public TileMapLayer TriggerRegion = null;

    public override HashSet<Vector2I> GetRegion()
    {
        if (TriggerRegion is null)
            return [];
        return [.. TriggerRegion.GetUsedCells()];
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (TriggerRegion is null || TriggerRegion.GetUsedCells().Count == 0)
            warnings.Add("No trigger region is defined. Behavior switch will never occur.");

        return [.. warnings];
    }
}