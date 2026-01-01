using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Layers;

public class SpecialActionRegionData
{
    public delegate void CellsUpdatedEventHandler(ISet<Vector2I> cells);

    private ImmutableHashSet<Vector2I> _cells = [];

    public event CellsUpdatedEventHandler CellsUpdated;

    public StringName Action = "";

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

    public readonly HashSet<Faction> AllowedFactions = [];

    public readonly HashSet<UnitData> AllowedUnits = [];

    public bool OneShot = false;

    public bool SingleUse = false;

    public ImmutableHashSet<UnitData> Performed = [];

    public bool CanPerform(UnitData unit) => (AllowedFactions.Contains(unit.Faction) || AllowedUnits.Contains(unit)) && (!SingleUse || !Performed.Contains(unit));

    public bool Perform(UnitData unit, Vector2I cell)
    {
        if (!Cells.Contains(cell) || !CanPerform(unit))
            return false;

        Performed = Performed.Add(unit);
        if (OneShot)
            Cells = Cells.Remove(cell);
        return true;
    }
}