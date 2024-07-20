using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Scenes.Level.Map;

namespace Scenes.Level.UI;

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

    /// <summary>The sets of used cells for the overlay.</summary>
    public IDictionary<string, ImmutableHashSet<Vector2I>> UsedCells
    {
        get => Enumerable.Range(0, GetLayersCount()).ToDictionary(GetLayerName, (i) => this[GetLayerName(i)]);
        set
        {
            for (int i = 0; i < GetLayersCount(); i++)
                this[GetLayerName(i)] = value[GetLayerName(i)];
        }
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

    /// <summary>Compute a <see cref="Rect2"/> that encloses all the cells that contain tiles in the given layer.</summary>
    /// <param name="grid">Grid defining cell sizes.</param>
    /// <param name="layer">Name of the layer.</param>
    /// <returns>A rectangle enclosing all of the used cells in the layer, or <c>null</c> if the layer is empty.</returns>
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

    /// <summary>Compute a <see cref="Rect2"/> that encloses all the used tiles among all layers in the overlay.</summary>
    /// <param name="grid">Grid defining cell sizes.</param>
    /// <returns>A rectangle enclosing all of the used cells in the overlay, or <c>null</c> if there are none.</returns>
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