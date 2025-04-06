using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.State;

namespace TbsTemplate.Scenes.Level.Map;

[Tool]
public partial class GridRenderer : Node2D
{
    private TileMapLayer _ground = null;

    private void UpdateTerrain()
    {
        if (TerrainLayer is null)
        {
            for (int r = 0; r < State.Size.Y; r++)
                for (int c = 0; c < State.Size.X; c++)
                    State.Terrain[r][c] = State.DefaultTerrain;
        }
        else
        {
            for (int r = 0; r < State.Size.Y; r++)
                for (int c = 0; c < State.Size.X; c++)
                    State.Terrain[r][c] = TerrainLayer.GetCellTileData(new Vector2I(c, r))?.GetCustomData("terrain").As<Terrain>() ?? State.DefaultTerrain;
        }
    }

    [Export] public GridState State = new();

    /// <summary><see cref="TileMapLayer"/> containing ground tiles.</summary>
    [Export] public TileMapLayer GroundLayer = null;

    /// <summary><see cref="TileMapLayer"/> layer containing terrain tiles. This is the layer that will be used to define terrain effects.</summary>
    [Export] public TileMapLayer TerrainLayer = null;

    /// <summary>Grid cell dimensions derived from the <see cref="TileSet"/>.  If there is no <see cref="TileSet"/>, the size is zero.</summary>
    public Vector2 CellSize => GroundLayer?.TileSet?.TileSize ?? Vector2.Zero;

    /// <summary>Regions in which units can perform special actions defined by the region.</summary>
    public IEnumerable<SpecialActionRegion> SpecialActionRegions => GetChildren().OfType<SpecialActionRegion>();

    /// <summary>Find the position in pixels of a cell offset.</summary>
    /// <param name="offset">Cell offset to use for calculation (can be outside grid bounds).</param>
    /// <returns>The position, in pixels, of the upper-left corner of the grid cell.</returns>
    public Vector2 PositionOf(Vector2I offset) => offset*CellSize;

    /// <summary>Find the cell offset of a pixel position.</summary>
    /// <param name="position">Position in world pixels.</param>
    /// <returns>The coordinates of the cell containing the pixel <paramref name="position"/> (can be outside grid bounds).</returns>
    public Vector2I CellOf(Vector2 position) => (Vector2I)(position/CellSize);

    /// <summary>Snap a position to a grid cell.</summary>
    /// <param name="position">Position in world pixels.</param>
    /// <returns>The position of the upper-left corner of the cell containing the given <paramref name="position"/>.</returns>
    public Vector2 Snap(Vector2 position) => PositionOf(CellOf(position));

    /// <param name="cell">Coordinates of the cell.</param>
    /// <returns>The bounding box of the cell.</returns>
    public Rect2 CellRect(Vector2I cell) => new(cell*CellSize, CellSize);

    /// <summary>Compute the smallest rectangle the encloses a set of cells.</summary>
    /// <param name="cells">Cells to enclose.</param>
    /// <returns>A rectangle enclosing all of the <paramref name="cells"/>, or <c>null</c> if the set is empty.</returns>
    public Rect2? EnclosingRect(IEnumerable<Vector2I> cells)
    {
        Rect2? enclosure = null;
        foreach (Vector2I cell in cells)
        {
            Rect2 cellRect = CellRect(cell);
            enclosure = enclosure?.Expand(cellRect.Position).Expand(cellRect.End) ?? cellRect;
        }
        return enclosure;
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (State is null)
            warnings.Add("There is no state to store data in.");

        if (GroundLayer is null)
            warnings.Add("No ground layer has been defined.");
        else
            foreach (TileMapLayer layer in GetChildren().OfType<TileMapLayer>())
                if ((layer.TileSet?.TileSize ?? Vector2.Zero) != CellSize)
                    warnings.Add($"Tile size of layer {layer.Name} does not match cell size {CellSize}");

        return [.. warnings];
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (!Engine.IsEditorHint() && @event is InputEventMouseButton m)
        {
            Vector2I cell = CellOf(m.Position);
            if (State.Contains(cell))
                GD.Print(State.Terrain[cell.Y][cell.X].ResourceName);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateTerrain();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint() && State is not null)
        {
            if (GroundLayer is not null)
                State.Size = GroundLayer.GetUsedRect().End;
            UpdateTerrain();
        }
    }
}