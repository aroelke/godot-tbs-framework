using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsTemplate.Data;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>Defines the grid dimensions and attributes and contains the locations of the objects and terrain within it.</summary>
[Tool]
public partial class Grid : Node2D, IGrid
{
    /// <summary><see cref="TileMapLayer"/> containing ground tiles.</summary>
    [Export] public TileMapLayer GroundLayer = null;

    /// <summary><see cref="TileMapLayer"/> layer containing terrain tiles. This is the layer that will be used to define terrain effects.</summary>
    [Export] public TileMapLayer TerrainLayer = null;

    /// <summary>Default terrain to use when it isn't placed explicitly on the map.</summary>
    [Export] public Terrain DefaultTerrain = null;

    /// <summary>Grid cell dimensions derived from the <see cref="TileSet"/>.  If there is no <see cref="TileSet"/>, the size is zero.</summary>
    public Vector2 CellSize => GroundLayer?.TileSet?.TileSize ?? Vector2.Zero;

    public Vector2I Size => GroundLayer?.GetUsedRect().End ?? Vector2I.Zero;

    /// <summary>Characters and objects occupying the grid.</summary>
    public readonly Dictionary<Vector2I, GridNode> Occupants = [];

    /// <summary>Regions in which units can perform special actions defined by the region.</summary>
    public IEnumerable<SpecialActionRegion> SpecialActionRegions => GetChildren().OfType<SpecialActionRegion>();

    /// <summary>Find the cell offset closest to the given one inside the grid.</summary>
    /// <param name="cell">Cell offset to clamp.
    /// <returns>The cell <paramref name="offset"/> clamped to be inside the grid bounds using <c>Vector2I.Clamp</c></returns>
    public Vector2I Clamp(Vector2I offset) => offset.Clamp(Vector2I.Zero, Size - Vector2I.One);

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

    public bool Contains(Vector2I cell) => IGrid.Contains(this, cell);
    public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out GridNode occupant) || (occupant is Unit unit && unit.Faction.AlliedTo(faction));
    public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => IGrid.GetCellsAtDistance(this, cell, distance);
    public Terrain GetTerrain(Vector2I cell) => TerrainLayer?.GetCellTileData(cell)?.GetCustomData("terrain").As<Terrain>() ?? DefaultTerrain;
    public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
    public IImmutableDictionary<Vector2I, IUnit> GetOccupantUnits() => Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => e.Value as IUnit);
    public IEnumerable<ISpecialActionRegion> GetSpecialActionRegions() => SpecialActionRegions;

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (DefaultTerrain is null)
            warnings.Add("No default terrain set");

        if (GroundLayer is null)
            warnings.Add("No ground layer has been defined.");
        else
            foreach (TileMapLayer layer in GetChildren().OfType<TileMapLayer>())
                if ((layer.TileSet?.TileSize ?? Vector2.Zero) != CellSize)
                    warnings.Add($"Tile size of layer {layer.Name} does not match cell size {CellSize}");

        return [.. warnings];
    }
}