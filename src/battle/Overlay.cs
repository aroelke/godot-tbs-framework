using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace battle;

/// <summary>Map overlay tile set for computing and displaying traversable and attackable cells and managing unit movement.</summary>
public partial class Overlay : TileMap
{
	/// <summary>Directions to look when finding cell neighbors.</summary>
	public static readonly Vector2I[] Directions = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };

	private readonly List<Vector2I> _path = new();
	private AStar2D _astar = new();

	/// <summary>Get all grid cells that a unit can walk on or pass through.</summary>
	/// <param name="map">Map the unit is walking on.</param>
    /// <param name="unit">Unit compute traversable cells for.</param>
    /// <returns>The list of cells, in any order, that the unit can traverse.</returns>
	public static Vector2I[] GetTraversableCells(BattleMap map, Unit unit)
    {
        Dictionary<Vector2I, int> cells = new() {{ unit.Cell, 0 }};
        Queue<Vector2I> potential = new();

        potential.Enqueue(unit.Cell);
        while (potential.Count > 0)
        {
            Vector2I current = potential.Dequeue();

            foreach (Vector2I direction in Directions)
            {
                Vector2I neighbor = current + direction;
                Terrain terrain = map.GetTerrain(neighbor);

                int cost = cells[current] + terrain.Cost;
                if (map.Contains(neighbor) && (!cells.ContainsKey(neighbor) || cells[neighbor] > cost) && cost <= unit.MoveRange)
                {
                    cells[neighbor] = cost;
                    potential.Enqueue(neighbor);
                }
            }
        }

        return cells.Keys.ToArray();
    }

	/// <summary>Most recently computed list of cells that can be traversed.</summary>
	public Vector2I[] TraversableCells { get; private set; } = Array.Empty<Vector2I>();

	/// <summary>Draw the cells that can be traversed.</summary>
	/// <param name="cells">List of cells that can be traversed, in any order.</param>
	public void DrawOverlay(Vector2I[] cells)
	{
		base.Clear();
		SetCellsTerrainConnect(0, new(cells), 0, 0);
	}

	/// <summary>Compute and draw the cells that a unit can traverse.</summary>
	/// <param name="map">Map containing the cells to be traversed.</param>
	/// <param name="unit">Unit traversing the cells.</param>
	public void DrawMoveRange(BattleMap map, Unit unit)
	{
		TraversableCells = GetTraversableCells(map, unit);
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

	/// <summary>In addition to clearing the overlay tiles, also clear the list of traversable cells.</summary>
	public new void Clear()
	{
		base.Clear();
		TraversableCells = Array.Empty<Vector2I>();
		_path.Clear();
		_astar.Clear();
	}

	public void AddToPath(BattleMap map, Unit unit, Vector2I cell)
	{
		_path.Add(cell);
		if (_path.Select((c) => map.GetTerrain(c).Cost).Sum() - map.GetTerrain(_path[0]).Cost > unit.MoveRange)
		{
			Vector2I start = _path[0];
			_path.Clear();
			_path.AddRange(_astar.GetPointPath(map.CellId(start), map.CellId(cell)).Select((c) => (Vector2I)c));
		}

		if (_path.Count > 0)
		{
			ClearLayer(1);
			if (_path.Count > 1)
				SetCellsTerrainConnect(1, new(_path), 1, 0);
		}
	}
}