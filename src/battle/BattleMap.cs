using Godot;
using level.map;
using level.unit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace battle;

/// <summary>Represents the battle map, containing its terrain and managing units and obstacles on it.</summary>
[Tool]
public partial class BattleMap : TileMap
{
    private Camera2D _camera;
    private readonly Dictionary<Vector2I, Unit> _units = new();
    private Unit _selected;
    private Overlay _overlay = null;
    private int _terrainLayer = -1;

    private Camera2D Camera => _camera ??= GetNode<Camera2D>("Pointer/BattleCamera");
    private Overlay Overlay => _overlay ??= GetNode<Overlay>("Overlay");

    private void DeselectUnit()
    {
        if (_selected != null)
        {
            _selected.IsSelected = false;
            _selected = null;
            Overlay.Clear();
        }
    }

    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    [Export] public Vector2I Size { get; private set; } = Vector2I.Zero;

    /// <summary>Default terrain to use when it isn't placed explicitly on the map.</summary>
    [Export] public Terrain DefaultTerrain;

    /// <summary>Grid cell dimensions derived from the tile set.  If there is no tileset, the size is zero.</summary>
    public Vector2I CellSize => TileSet?.TileSize ?? Vector2I.Zero;

    /// <summary>
    /// Check if a cell offset is in the grid.
    /// </summary>
    /// <param name="offset">offset to check.</param>
    /// <returns><c>true</c> if the offset is within the grid bounds, and <c>false</c> otherwise.</returns>
    public bool Contains(Vector2I offset) => offset.X >= 0 && offset.X < Size.X && offset.Y >= 0 && offset.Y < Size.Y;

    /// <summary>Find the cell offset closest to the given one inside the grid.</summary>
    /// <param name="cell">Cell offset to clamp.
    /// <returns>The cell offset clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I cell) => cell.Clamp(Vector2I.Zero, Size - Vector2I.One);

    /// <summary>Constrain a position to somewhere within the grid (not necessarily snapped to a cell).</summary>
    /// <param name="position">Position to clamp.</param>
    /// <returns>The world position clamped to be inside the grid using <c>Vector2.Clamp</c></returns>
    public Vector2 Clamp(Vector2 position) => position.Clamp(Vector2.Zero, Size*CellSize - Vector2.One);

    /// <summary>Find the position in pixels of a cell offset.</summary>
    /// <param name="offset">Cell offset to use for calculation (can be outside grid bounds).</param>
    /// <returns>The position, in pixels of the upper-left corner of the grid cell.</returns>
    public Vector2I PositionOf(Vector2I offset) => offset*CellSize;

    /// <summary>Find the cell offset of a pixel position.</summary>
    /// <param name="pixels">Position in pixels.</param>
    /// <returns>The coordinates of the cell containing the pixel point (can be outside grid bounds).</returns>
    public Vector2I CellOf(Vector2 point) => (Vector2I)(point/CellSize);

    /// <returns>The linear ID of the grid cell.</returns>
    public int CellId(Vector2I cell) => cell.X*Size.X + cell.Y;

    /// <returns>The terrain information for a cell, or <c>DefaultTerrain</c> if the terrain hasn't been set.</returns>
    /// <exception cref="IndexOutOfRangeException">If the cell is outside the grid.</exception>
    public Terrain GetTerrain(Vector2I cell) => GetCellTileData(_terrainLayer, cell)?.GetCustomData("terrain").As<Terrain>() ?? DefaultTerrain;

    /// <summary>When the cursor moves, if there's a selected unit, draw its path.</summary>
    /// <param name="previous">Previous location of the cursor.</param>
    /// <param name="current">Current location of the cursor.</param>
    public void OnCursorMoved(Vector2I previous, Vector2I current)
    {
        if (_selected != null && Overlay.TraversableCells.Contains(current))
            Overlay.AddToPath(this, _selected, current);
    }

    /// <summary>
    /// Act on the selected cell: If there isn't a unit selected, select one. If there is and the selected cell is one it can move to,
    /// move it to that cell.  Afterwards or otherwise, deselect the unit.
    /// </summary>
    /// <param name="cell">Cell to select.</param>
    public async void OnCellCelected(Vector2I cell)
    {
        if (_selected != null)
        {
            if (!_selected.IsMoving)
            {
                if (cell != _selected.Cell && Overlay.TraversableCells.Contains(cell))
                {
                    _units.Remove(_selected.Cell);
                    _selected.MoveAlong(Overlay.Path);
                    _units[_selected.Cell] = _selected;
                    Overlay.Clear();
                    await ToSignal(_selected, Unit.SignalName.DoneMoving);
                    IEnumerable<Vector2I> localAttack = Overlay.GetCellsInRange(this, _selected.AttackRange, _selected.Cell).Where((c) => c != _selected.Cell);
                    IEnumerable<Vector2I> localSupport = Overlay.GetCellsInRange(this, _selected.SupportRange, _selected.Cell);
                    Overlay.DrawOverlay(Overlay.AttackLayer, localAttack);
                    Overlay.DrawOverlay(Overlay.SupportLayer, localSupport.Where((c) => !localAttack.Contains(c)));
                }
                else
                    DeselectUnit();
            }
        }
        else
        {
            if (_units.ContainsKey(cell) && _units[cell] != _selected)
            {
                _selected = _units[cell];
                _selected.IsSelected = true;
                Overlay.DrawMoveRange(this, _selected);
            }
            else if (_selected != null)
                DeselectUnit();
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        // Size dimensions should be nonnegative
        if (Size.X <= 0 || Size.Y <= 0)
            warnings.Add($"Grid size {Size} has illegal dimensions.");

        // Tiles should be within the grid
        for (int i = 0; i < GetLayersCount(); i++)
            foreach (Vector2I cell in GetUsedCells(i))
                if (cell.X < 0 || cell.X >= Size.X || cell.Y < 0 || cell.Y >= Size.Y)
                    warnings.Add($"There is a tile on layer {GetLayerName(i)} placed outside the grid bounds at {cell}");
        
        // A default terrain should be set
        if (DefaultTerrain == null)
            warnings.Add("There is no default terrain");

        return warnings.ToArray();
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            (Camera.LimitTop, Camera.LimitLeft) = Vector2I.Zero;
            (Camera.LimitRight, Camera.LimitBottom) = Size*CellSize;

            _units.Clear();
            foreach (Node child in GetChildren())
                if (child is Unit unit)
                    _units[unit.Cell] = unit;

            for (int i = 0; i < GetLayersCount(); i++)
            {
                if (GetLayerName(i) == "terrain")
                {
                    _terrainLayer = i;
                    break;
                }
            }
        }
    }
}