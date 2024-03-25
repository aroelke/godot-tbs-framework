using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Level.Map;

namespace Level.UI;

/// <summary>Map overlay tile set for displaying traversable and attackable cells and managing unit movement.</summary>
public partial class Overlay : TileMap
{
    // TileSet source ID for the path arrows and indices containing arrowheads.
    // XXX: DEPENDS STRONGLY ON TILESET ORGANIZATION
    private const int PathSourceId = 3;
    private static readonly Vector2I RightArrow = new(6, 0);
    private static readonly Vector2I DownArrow = new(7, 0);
    private static readonly Vector2I UpArrow = new(6, 1);
    private static readonly Vector2I LeftArrow = new(7, 1);

    /// <summary>Get the index of a string property in a list of properties, such as a tile set layer name.</summary>
    /// <param name="property">Function for getting the string element of each item in the list.</param>
    /// <param name="count">Number of items in the list.</param>
    /// <param name="name">String to search for.</param>
    /// <returns>The index in the list property of the item whose string element matches the desired value.</returns>
    private static int GetIndex(Func<int, string> property, int count, string name)
    {
        for (int i = 0; i < count; i++)
            if (property(i) == name)
                return i;
        return -1;
    }

    private int _selectionSet = -1, _selectionTerrain = -1;
    private int _pathLayer = -1, _pathSet = 1, _pathTerrain = -1;
    private int _traverseLayer = -1, _attackLayer = -1, _supportLayer = -1;

    /// <summary>Draw the cells that can be traversed.</summary>
    /// <param name="cells">List of cells that can be traversed, in any order.</param>
    private void DrawOverlay(int layer, IEnumerable<Vector2I> cells)
    {
        ClearLayer(layer);
        SetCellsTerrainConnect(layer, new(cells), _selectionSet, _selectionTerrain);
    }

    /// <summary>Collection of traversable cells drawn on the map.</summary>
    public IEnumerable<Vector2I> TraversableCells
    {
        get => GetUsedCells(_traverseLayer);
        set => DrawOverlay(_traverseLayer, value);
    }

    /// <summary>Collection of attackable cells drawn on the map.</summary>
    public IEnumerable<Vector2I> AttackableCells
    {
        get => GetUsedCells(_attackLayer);
        set => DrawOverlay(_attackLayer, value);
    }

    /// <summary>Collection of supportable cells drawn on the map.</summary>
    public IEnumerable<Vector2I> SupportableCells
    {
        get => GetUsedCells(_supportLayer);
        set => DrawOverlay(_supportLayer, value);
    }

    /// <summary>List of cells defining a movement path.</summary>
    public List<Vector2I> Path
    {
        get => GetUsedCells(_pathLayer).ToList();
        set
        {
            ClearLayer(_pathLayer);
            if (value.Count > 1)
            {
                SetCellsTerrainPath(_pathLayer, new(value), _pathSet, _pathTerrain);
                SetCell(_pathLayer, value.Last(), PathSourceId, (value[^1] - value[^2]) switch
                {
                    Vector2I(0, >0) => DownArrow,
                    Vector2I(>0, 0) => RightArrow,
                    Vector2I(0, <0) => UpArrow,
                    Vector2I(<0, 0) => LeftArrow,
                    _ => new(8, 0)
                });
            }
        }
    }

    /// <summary> Compute the rectangle the encloses the movement, support, and attack ranges of the overlay. </summary>
    /// <param name="grid">Grid defining cell sizes that the rectangle encloses.</param>
    /// <returns>The minimum rectangle that encloses all the cells, or <c>null</c> if all the ranges are empty.</returns>
    public Rect2? GetEnclosingRect(Grid grid)
    {
        Rect2? enclosure = null;
        void ExpandZoomRect(IEnumerable<Vector2I> cells)
        {
            foreach (Vector2I c in cells)
            {
                Rect2 cellRect = grid.CellRect(c);
                enclosure = enclosure?.Expand(cellRect.Position).Expand(cellRect.End) ?? cellRect;
            }
        }
        ExpandZoomRect(TraversableCells);
        ExpandZoomRect(AttackableCells);
        ExpandZoomRect(SupportableCells);
        return enclosure;
    }

    public override void _Ready()
    {
        base._Ready();

        _traverseLayer = GetIndex(GetLayerName, GetLayersCount(), "move");
        _attackLayer = GetIndex(GetLayerName, GetLayersCount(), "attack");
        _supportLayer = GetIndex(GetLayerName, GetLayersCount(), "support");
        for (int i = 0; i < TileSet.GetTerrainSetsCount(); i++)
        {
            _selectionTerrain = GetIndex((t) => TileSet.GetTerrainName(i, t), TileSet.GetTerrainsCount(i), "selection");
            if (_selectionTerrain != -1)
            {
                _selectionSet = i;
                break;
            }
        }

        _pathLayer = GetIndex(GetLayerName, GetLayersCount(), "path");
        for (int i = 0; i < TileSet.GetTerrainSetsCount(); i++)
        {
            _pathTerrain = GetIndex((t) => TileSet.GetTerrainName(i, t), TileSet.GetTerrainsCount(i), "path");
            if (_pathTerrain != -1)
            {
                _pathSet = i;
                break;
            }
        }
    }
}