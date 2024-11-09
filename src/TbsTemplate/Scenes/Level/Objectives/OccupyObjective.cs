using System.Collections.Generic;
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

    /// <summary>Army that should occupy the region. May be <c>null</c> if <see cref="Unit"/> is not.</summary>
    [Export] public Army Army = null;

    /// <summary>Unit that should occupy the region. May be <c>null</c> if <see cref="Army"/> is not.</summary>
    [Export] public Unit Unit = null;

    public override bool Complete
    {
        get
        {
            if (Region is null)
                return false;
            
            HashSet<Vector2I> region = [.. Region.GetUsedCells()];
            if (Army is not null)
                foreach (Unit unit in (IEnumerable<Unit>)Army)
                    if (region.Contains(unit.Cell))
                        return true;
            return Unit is not null && region.Contains(Unit.Cell);
        }
    }

    public override string Description => Region is null ? "" : (Army, Unit) switch {
        (not null, not null) => $"{Army.Name} or {Unit.Name} occupies {Region.Name}",
        (not null, null)     => $"{Army.Name} occupies {Region.Name}",
        (null,     not null) => $"{Unit.Name} occupies {Region.Name}",
        (null,     null)     => ""
    };

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Region is null || Region.GetUsedCells().Count == 0)
            warnings.Add("Region is undefined or empty. Objective cannot be completed.");
        if (Army is null && Unit is null)
            warnings.Add("No units to enter region have been defined. Objective cannot be completed.");

        return [.. warnings];
    }
}