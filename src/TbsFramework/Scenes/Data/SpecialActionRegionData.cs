using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace TbsFramework.Scenes.Data;

/// <summary>Data specifying the information about a region in the grid in which a unit can perform a special action.</summary>
public class SpecialActionRegionData : IHasIdentity<ActionRegionIdentity, SpecialActionRegionData>
{
    /// <summary>Handler for changes the cells defining the region in which the action can be performed.</summary>
    public delegate void CellsUpdatedEventHandler(ISet<Vector2I> cells);

    private GridData _grid = null;
    private ImmutableHashSet<Vector2I> _cells = [];

    private void OnGridSizeChanged(Vector2I _, Vector2I size)
    {
        if (!_cells.All(_grid.Contains))
        {
            GD.PushWarning($"Some cells in region {Action} are outside the new grid bounds. They will be truncated.");
            Cells = [.. Cells.Where(_grid.Contains)];
        }
    }

    /// <summary>Event signaling that the cells defining the region have changed.</summary>
    public event CellsUpdatedEventHandler CellsUpdated;

    /// <summary>Name of the region. Also is the string displayed when presenting the option to perform the action.</summary>
    public StringName Action = "";

    /// <summary>Grid containing the action region.</summary>
    public GridData Grid
    {
        get => _grid;
        set
        {
            if (_grid != value)
            {
                if (_grid is not null)
                    _grid.SizeUpdated -= OnGridSizeChanged;
                _grid = value;
                if (_grid is not null)
                {
                    _grid.SizeUpdated += OnGridSizeChanged;
                    OnGridSizeChanged(Vector2I.Zero, _grid.Size);
                }
            }
        }
    }

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

    /// <summary>Units that have performed the action.</summary>
    public Dictionary<UnitIdentity, int> Performed = [];

    public ActionRegionIdentity Identity { get; set; } = null;

    public SpecialActionRegionData() {}

    private SpecialActionRegionData(SpecialActionRegionData original)
    {
        Action = original.Action;
        _cells = original._cells;
        Performed = original.Performed;
    }

    /// <returns>A copy of this special action region, including the units that have performed the action.</returns>
    public SpecialActionRegionData Clone() => new(this);
}