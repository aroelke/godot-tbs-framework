using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;

namespace battle;

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

	/// <summary>Join two lists of cell coordinates, removing the first loop if there is one.</summary>
	/// <param name="a">First coordinate list to join.</param>
	/// <param name="b">Second coordinate list to join.</param>
	/// <returns>A list of coordinates consisting of the two lists concatenated, but with the first loop removed.</returns>
	private static List<Vector2I> Join(IList<Vector2I> a, IList<Vector2I> b)
	{
		List<Vector2I> result = new();
		for (int i = 0; i < a.Count; i++)
		{
			for (int j = 0; j < b.Count; j++)
			{
				if (a[i] == b[j])
				{
					result.AddRange(a.Take(i));
					result.AddRange(b.TakeLast(b.Count - j));
					return result;
				}
			}
		}
		result.AddRange(a);
		result.AddRange(b);
		return result;
	}

	/// <summary>Directions to look when finding cell neighbors.</summary>
	public static readonly Vector2I[] Directions = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };

	/// <summary>Determine if two cell coordinate pairs are adjacent.</summary>
	/// <param name="a">First pair for comparison.</param>
	/// <param name="b">Second pair for comparison.</param>
	/// <returns><c>true</c> if the two coordinate pairs are adjacent, and <c>false</c> otherwise.</returns>
	public static bool IsAdjacent(Vector2I a, Vector2I b)
	{
		foreach (Vector2I direction in Directions)
			if (b - a == direction || a - b == direction)
				return true;
		return false;
	}

	/// <summary>Get all grid cells that a unit can walk on or pass through.</summary>
	/// <param name="map">Map the unit is walking on.</param>
    /// <param name="unit">Unit compute traversable cells for.</param>
    /// <returns>The set of cells, in any order, that the unit can traverse.</returns>
	public static IEnumerable<Vector2I> GetTraversableCells(BattleMap map, Unit unit)
    {
		int max = 2*(unit.MoveRange + 1)*(unit.MoveRange + 1) - 2*unit.MoveRange - 1;

		Dictionary<Vector2I, int> cells = new(max) {{ unit.Cell, 0 }};
        Queue<Vector2I> potential = new(max);

        potential.Enqueue(unit.Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I direction in Directions)
            {
                Vector2I neighbor = current + direction;
				if (map.Contains(neighbor))
				{
					int cost = cells[current] + map.GetTerrain(neighbor).Cost;
					if ((!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && cost <= unit.MoveRange)
					{
						cells[neighbor] = cost;
						potential.Enqueue(neighbor);
					}
				}
            }
        }

        return cells.Keys;
    }

	private int _moveLayer = -1;
	private int _selectionSet = -1, _selectionTerrain = -1;
	private int _pathLayer = -1, _pathSet = 1, _pathTerrain = -1;
	private List<Vector2I> _path = new();
	private AStar2D _astar = new();

	/// <summary>Most recently computed list of cells that can be traversed.</summary>
	public HashSet<Vector2I> TraversableCells { get; private set; } = new();

	/// <summary>The current path being drawn on the screen.</summary>
	public Vector2I[] Path => _path.ToArray();

	/// <summary>Draw the cells that can be traversed.</summary>
	/// <param name="cells">List of cells that can be traversed, in any order.</param>
	public void DrawOverlay(IEnumerable<Vector2I> cells)
	{
		base.Clear();
		SetCellsTerrainConnect(_moveLayer, new(cells), _selectionSet, _selectionTerrain);
	}

	/// <summary>Compute and draw the cells that a unit can traverse.</summary>
	/// <param name="map">Map containing the cells to be traversed.</param>
	/// <param name="unit">Unit traversing the cells.</param>
	public void DrawMoveRange(BattleMap map, Unit unit)
	{
		TraversableCells = new(GetTraversableCells(map, unit));
		_astar.Clear();
		foreach (Vector2I cell in TraversableCells)
			_astar.AddPoint(map.CellId(cell), cell, map.GetTerrain(cell).Cost);
		foreach (Vector2I cell in TraversableCells)
		{
			foreach (Vector2I direction in Directions)
			{
				Vector2I neighbor = cell + direction;
				if (!_astar.ArePointsConnected(map.CellId(cell), map.CellId(neighbor)) && TraversableCells.Contains(neighbor))
					_astar.ConnectPoints(map.CellId(cell), map.CellId(neighbor));
			}
		}

		DrawOverlay(TraversableCells);
		AddToPath(map, unit, unit.Cell);
	}

	/// <summary>
	/// Add a point to the end of the walking path.  If this results in the cost of the path being too long for the unit, recompute
	/// the entire path to be the shortest distance to the point.  Also remove any loops in the resulting path.
	/// </summary>
	/// <param name="map">Map containing the cells to move on.</param>
	/// <param name="unit">Unit the path is being computed for.</param>
	/// <param name="cell">Cell to add to the end of the path.</param>
	public void AddToPath(BattleMap map, Unit unit, Vector2I cell)
	{
		List<Vector2I> extension = new();
		if (_path.Count == 0 || IsAdjacent(cell, _path.Last()))
			extension.Add(cell);
		else
			extension.AddRange(_astar.GetPointPath(map.CellId(_path.Last()), map.CellId(cell)).Select((c) => (Vector2I)c));

		_path = Join(_path, extension);
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
		_moveLayer = GetIndex(GetLayerName, GetLayersCount(), "move");
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