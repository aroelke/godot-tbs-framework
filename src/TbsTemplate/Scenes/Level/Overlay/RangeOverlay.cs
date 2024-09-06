using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Map;

namespace TbsTemplate.Scenes.Level.Overlay;

/// <summary>A <see cref="Grid"/> overlay used to display information about the grid cells.</summary>
public partial class RangeOverlay : Node2D
{
    /// <summary>The sets of used cells for the overlay.</summary>
    public IDictionary<string, ImmutableHashSet<Vector2I>> UsedCells
    {
        get => GetChildren().OfType<TileMapLayer>().ToDictionary((l) => l.Name.ToString(), (l) => this[l.Name]); //Enumerable.Range(0, GetLayersCount()).ToDictionary(GetLayerName, (i) => this[GetLayerName(i)]);
        set
        {
            foreach ((string name, ImmutableHashSet<Vector2I> cells) in value)
                this[name] = cells;
        }
    }

    /// <summary>Cells used for a particular layer of the overlay.</summary>
    /// <param name="layer">Name of the layer.</param>
    /// <returns>The set of cells used for the layer.</returns>
    public ImmutableHashSet<Vector2I> this[string layer]
    {
        get => [.. GetNode<TileMapLayer>(layer).GetUsedCells()];
        set
        {
            GetNode<TileMapLayer>(layer).Clear();
            GetNode<TileMapLayer>(layer).SetCellsTerrainConnect(new(value), 0, 0);
        }
    }

    public void Clear()
    {
        foreach (TileMapLayer layer in GetChildren().OfType<TileMapLayer>())
            layer.Clear();
    }

    /// <summary>Compute a <see cref="Rect2"/> that encloses all the cells that contain tiles in the given layer.</summary>
    /// <param name="grid">Grid defining cell sizes.</param>
    /// <param name="layer">Name of the layer.</param>
    /// <returns>A rectangle enclosing all of the used cells in the layer, or <c>null</c> if the layer is empty.</returns>
    public Rect2? GetEnclosingRect(Grid grid, string layer)
    {
        Rect2? enclosure = null;
        foreach (Vector2I c in GetNode<TileMapLayer>(layer).GetUsedCells())
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
        foreach (TileMapLayer layer in GetChildren().OfType<TileMapLayer>())
        {
            Rect2? layerRect = GetEnclosingRect(grid, layer.Name);
            if (layerRect is not null)
                enclosure = enclosure?.Expand(layerRect.Value.Position).Expand(layerRect.Value.End) ?? layerRect;
        }
        return enclosure;
    }
}