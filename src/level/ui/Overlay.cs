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
    private List<Vector2I> _path = new();
    private AStar2D _astar = new();

    /// <summary>TileMap layer index to draw traversable cells on.</summary>
    public int TraverseLayer { get; private set; } = -1;

    /// <summary>TileMap layer index to draw attackable cells on.</summary>
    public int AttackLayer { get; private set; } = -1;

    /// <summary>TileMap layer index to draw supportable cells on.</summary>
    public int SupportLayer { get; private set; } = -1;

    /// <summary>Most recently computed list of cells that can be traversed.</summary>
    public HashSet<Vector2I> TraversableCells { get; private set; } = new();

    /// <summary>Most recently computed list of cells that can be attacked.</summary>
    public HashSet<Vector2I> AttackableCells { get; private set; } = new();

    /// <summary>Most recelty computed list of cells that can be supported.</summary>
    public HashSet<Vector2I> SupportableCells { get; private set; } = new();

    /// <summary>The current path being drawn on the screen.</summary>
    public Vector2I[] Path => _path.ToArray();

    /// <summary>Draw the cells that can be traversed.</summary>
    /// <param name="cells">List of cells that can be traversed, in any order.</param>
    public void DrawOverlay(int layer, IEnumerable<Vector2I> cells)
    {
        ClearLayer(layer);
        SetCellsTerrainConnect(layer, new(cells), _selectionSet, _selectionTerrain);
    }

    /// <summary>Compute and draw the cells that a unit can traverse.</summary>
    /// <param name="map">Map containing the cells to be traversed.</param>
    /// <param name="unit">Unit traversing the cells.</param>
    public void DrawMoveRange(LevelMap map, Unit unit)
    {
        TraversableCells = new(PathFinder.GetTraversableCells(map, unit));
        _astar.Clear();
        foreach (Vector2I cell in TraversableCells)
            _astar.AddPoint(map.CellId(cell), cell, map.GetTerrain(cell).Cost);
        foreach (Vector2I cell in TraversableCells)
        {
            foreach (Vector2I direction in PathFinder.Directions)
            {
                Vector2I neighbor = cell + direction;
                if (!_astar.ArePointsConnected(map.CellId(cell), map.CellId(neighbor)) && TraversableCells.Contains(neighbor))
                    _astar.ConnectPoints(map.CellId(cell), map.CellId(neighbor));
            }
        }
        AttackableCells = new(PathFinder.GetCellsInRange(map, unit.AttackRange, TraversableCells));
        SupportableCells = new(PathFinder.GetCellsInRange(map, unit.SupportRange, TraversableCells));

        DrawOverlay(TraverseLayer, TraversableCells);
        DrawOverlay(AttackLayer, AttackableCells.Where((c) => !TraversableCells.Contains(c)));
        DrawOverlay(SupportLayer, SupportableCells.Where((c) => !TraversableCells.Contains(c) && !AttackableCells.Contains(c)));
        AddToPath(map, unit, unit.Cell);
    }

    /// <summary>
    /// Add a point to the end of the walking path.  If this results in the cost of the path being too long for the unit, recompute
    /// the entire path to be the shortest distance to the point.  Also remove any loops in the resulting path.
    /// </summary>
    /// <param name="map">Map containing the cells to move on.</param>
    /// <param name="unit">Unit the path is being computed for.</param>
    /// <param name="cell">Cell to add to the end of the path.</param>
    public void AddToPath(LevelMap map, Unit unit, Vector2I cell)
    {
        List<Vector2I> extension = new();
        if (_path.Count == 0 || PathFinder.IsAdjacent(cell, _path.Last()))
            extension.Add(cell);
        else
            extension.AddRange(_astar.GetPointPath(map.CellId(_path.Last()), map.CellId(cell)).Select((c) => (Vector2I)c));

        _path = PathFinder.Join(_path, extension);
        if (_path.Select((c) => map.GetTerrain(c).Cost).Sum() - map.GetTerrain(_path[0]).Cost > unit.MoveRange)
        {
            Vector2I start = _path[0];
            _path.Clear();
            _path.AddRange(_astar.GetPointPath(map.CellId(start), map.CellId(cell)).Select((c) => (Vector2I)c));
        }

        ClearLayer(_pathLayer);
        if (_path.Count > 1)
        {
            SetCellsTerrainPath(_pathLayer, new(_path), _pathSet, _pathTerrain);
            SetCell(_pathLayer, _path.Last(), PathSourceId, (_path[^1] - _path[^2]) switch
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
        _path.Clear();
        _astar.Clear();
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