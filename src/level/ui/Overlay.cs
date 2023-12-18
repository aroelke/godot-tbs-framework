using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;
using level.unit;
using util;

namespace level.ui;

/// <summary>Map overlay tile set for computing and displaying traversable and attackable cells and managing unit movement.</summary>
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

    /// <summary>TileMap layer index to draw traversable cells on.</summary>
    public int TraverseLayer { get; private set; } = -1;

    /// <summary>TileMap layer index to draw attackable cells on.</summary>
    public int AttackLayer { get; private set; } = -1;

    /// <summary>TileMap layer index to draw supportable cells on.</summary>
    public int SupportLayer { get; private set; } = -1;

    /// <summary>Most recently computed list of cells that can be traversed.</summary>
    public HashSet<Vector2I> TraversableCells { get; private set; } = new();

    /// <summary>Draw the cells that can be traversed.</summary>
    /// <param name="cells">List of cells that can be traversed, in any order.</param>
    public void DrawOverlay(int layer, IEnumerable<Vector2I> cells)
    {
        ClearLayer(layer);
        SetCellsTerrainConnect(layer, new(cells), _selectionSet, _selectionTerrain);
    }

    /// <summary>Draw a movement path on the map.</summary>
    /// <param name="path">List of cells defining the path to draw.</param>
    public void DrawPath(List<Vector2I> path)
    {
        ClearLayer(_pathLayer);
        if (path.Count > 1)
        {
            SetCellsTerrainPath(_pathLayer, new(path), _pathSet, _pathTerrain);
            SetCell(_pathLayer, path.Last(), PathSourceId, (path[^1] - path[^2]) switch
            {
                Vector2I(0, >0) => DownArrow,
                Vector2I(>0, 0) => RightArrow,
                Vector2I(0, <0) => UpArrow,
                Vector2I(<0, 0) => LeftArrow,
                _ => new(8, 0)
            });
        }
    }

    /// <summary>In addition to clearing the overlay tiles, also clear the list of traversable cells.</summary>
    public new void Clear()
    {
        base.Clear();
        TraversableCells.Clear();
    }

    public override void _Ready()
    {
        base._Ready();

        TraverseLayer = GetIndex(GetLayerName, GetLayersCount(), "move");
        AttackLayer = GetIndex(GetLayerName, GetLayersCount(), "attack");
        SupportLayer = GetIndex(GetLayerName, GetLayersCount(), "support");
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