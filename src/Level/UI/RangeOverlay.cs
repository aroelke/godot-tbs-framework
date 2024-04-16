using System.Collections.Immutable;
using Godot;
using Level.Map;

namespace Level.UI;

/// <summary>A <see cref="Grid"/> overlay used to display information about the grid cells.</summary>
public partial class RangeOverlay : TileMap
{
    /// <summary>Get the index of the layer of the given name.</summary>
    private int GetLayerIndex(string name)
    {
        for (int i = 0; i < GetLayersCount(); i++)
            if (GetLayerName(i) == name)
                return i;
        return -1;
    }

    /// <summary>Cells used for a particular layer of the overlay.</summary>
    /// <param name="layer">Name of the layer.</param>
    /// <returns>The set of cells used for the layer.</returns>
    public ImmutableHashSet<Vector2I> this[string layer]
    {
        get => GetUsedCells(GetLayerIndex(layer)).ToImmutableHashSet();
        set
        {
            int index = GetLayerIndex(layer);
            ClearLayer(index);
            SetCellsTerrainConnect(index, new(value), 0, 0);
        }
    }

    public Rect2? GetEnclosingRect(Grid grid, string layer)
    {
        Rect2? enclosure = null;
        foreach (Vector2I c in GetUsedCells(GetLayerIndex(layer)))
        {
            Rect2 cellRect = grid.CellRect(c);
            enclosure = enclosure?.Expand(cellRect.Position).Expand(cellRect.End) ?? cellRect;
        }
        return enclosure;
    }

    public Rect2? GetEnclosingRect(Grid grid)
    {
        Rect2? enclosure = null;
        for (int i = 0; i < GetLayersCount(); i++)
        {
            Rect2? layerRect = GetEnclosingRect(grid, GetLayerName(i));
            if (layerRect is not null)
                enclosure = enclosure?.Expand(layerRect.Value.Position).Expand(layerRect.Value.End) ?? layerRect;
        }
        return enclosure;
    }
}