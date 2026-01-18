using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Layers;

/// <summary>Data specifying the information about a region in the grid in which a unit can perform a special action.</summary>
public class SpecialActionRegionData
{
    /// <summary>Handler for changes the cells defining the region in which the action can be performed.</summary>
    public delegate void CellsUpdatedEventHandler(ISet<Vector2I> cells);

    private ImmutableHashSet<Vector2I> _cells = [];

    /// <summary>Event signaling that the cells defining the region have changed.</summary>
    public event CellsUpdatedEventHandler CellsUpdated;

    /// <summary>Name of the region. Also is the string displayed when presenting the option to perform the action.</summary>
    public StringName Action = "";

    /// <summary>Cells on the grid in which the special action can be performed.</summary>
    public ImmutableHashSet<Vector2I> Cells
    {
        get => _cells;
        set
        {
            if (_cells != value)
            {
                _cells = value;
                if (CellsUpdated is not null)
                    CellsUpdated(_cells);
            }
        }
    }

    /// <summary>Factions containing units that are allowed to perform the action, even if they haven't spawned yet.</summary>
    public readonly HashSet<Faction> AllowedFactions = [];

    /// <summary>Specific units that are allowed to perform the action.</summary>
    public readonly HashSet<UnitData> AllowedUnits = [];

    /// <summary>Whether or not the action can only be performed once per cell. Performing the action removes that cell from <see cref="Cells"/>.</summary>
    public bool OneShot = false;

    /// <summary>Whether or not each allowed unit can perform the action only a single time.  Does not update <see cref="AllowedUnits"/> when performed.</summary>
    public bool SingleUse = false;

    /// <summary>Units that have performed the action. If <see cref="SingleUse"/> is <c>true</c>, those units can't perform it again.</summary>
    public ImmutableHashSet<UnitData> Performed = [];

    public SpecialActionRegionData() {}

    private SpecialActionRegionData(SpecialActionRegionData original)
    {
        Action = original.Action;
        _cells = original._cells;
        OneShot = original.OneShot;
        SingleUse = original.SingleUse;
        AllowedFactions = [.. original.AllowedFactions];
        AllowedUnits = [.. original.AllowedUnits];
        Performed = original.Performed;
    }

    /// <returns><c>true</c> if <paramref name="unit"/> can perform the special action, and <c>false</c> otherwise.</returns>
    public bool CanPerform(UnitData unit) => (AllowedFactions.Contains(unit.Faction) || AllowedUnits.Contains(unit)) && (!SingleUse || !Performed.Contains(unit));

    /// <summary>Update the region to reflect that a unit has performed the special action.</summary>
    /// <param name="unit">Unit performing the action.</param>
    /// <param name="cell">Cell in which the unit is performing the action.</param>
    /// <returns>
    /// <c>true</c> if the region was updated, which happens if <paramref name="unit"/> is allowed to perform the action and <paramref name="cell"/> is one
    /// contained by the region, and <c>false</c> otherwise.
    /// </returns>
    public bool Perform(UnitData unit, Vector2I cell)
    {
        if (!Cells.Contains(cell) || !CanPerform(unit))
            return false;

        Performed = Performed.Add(unit);
        if (OneShot)
            Cells = Cells.Remove(cell);
        return true;
    }

    /// <returns>A copy of this special action region, including the units that have performed the action.</returns>
    public SpecialActionRegionData Clone() => new(this);
}