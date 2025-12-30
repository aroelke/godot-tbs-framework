using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;

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
}