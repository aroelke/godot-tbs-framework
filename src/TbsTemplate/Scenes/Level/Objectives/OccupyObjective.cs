using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that's accomplished when a specific unit and/or a unit from a specific army occupy a specified region.</summary>
[Tool]
public partial class OccupyObjective : Objective
{
    /// <summary>Region representing the spaces the target unit(s) should occupy.</summary>
    [Export] public TileMapLayer Region = null;

    /// <summary>Army that should occupy the region. May be <c>null</c> if <see cref="Units"/> is not empty.</summary>
    [Export] public Army Army = null;

    /// <summary>Units that should occupy the region. May be empty if <see cref="Army"/> is not <c>null</c>.</summary>
    [Export] public Unit[] Units = [];

    /// <summary>Number of units from the set that must occupy the region.</summary>
    [Export(PropertyHint.Range, "1,10,or_greater")] public int Count = 1;

    public override bool Complete
    {
        get
        {
            if (Region is null)
                return false;

            int occupants = 0;
            HashSet<Vector2I> region = [.. Region.GetUsedCells()];
            if (Army is not null)
                occupants += region.Where((c) => ((IEnumerable<Unit>)Army).Any((u) => u.Cell == c)).Count();
            occupants += region.Where((c) => Units.Any((u) => u.Cell == c)).Count();
            return occupants >= Count;
        }
    }

    public override string Description => Region is null ? "" : (Army, Units.Count((u) => u is not null)) switch {
        (not null, >0) => $"{Count} of {Army.Name} or {string.Join(",", Units.Where((u) => u is not null).Select((u) => u.Name))} occupies {Region.Name}",
        (not null,  0) => $"{Count} of {Army.Name} occupies {Region.Name}",
        (null,     >0) => $"{Count} of {string.Join(",", Units.Where((u) => u is not null).Select((u) => u.Name))} occupies {Region.Name}",
        (null,      0) => "",
        _              => ""
    };

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Region is null || Region.GetUsedCells().Count == 0)
            warnings.Add("Region is undefined or empty. Objective cannot be completed.");
        if (Region is not null && Region.GetUsedCells().Count < Count)
            warnings.Add("Not enough spaces in region to occupy. Objective cannot be completed.");
        if (Army is null)
        {
            if (Units.Length == 0)
                warnings.Add("No units to enter region have been defined. Objective cannot be completed.");
            else if (Units.Length < Count)
                warnings.Add("Not enough units in list to occupy region. Objective cannot be completed.");
        }
        if (Units.Any((u) => u is null))
            warnings.Add($"Undefined unit in specific unit list.");

        return [.. warnings];
    }
}