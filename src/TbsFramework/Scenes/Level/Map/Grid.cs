using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Map;

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

    [Export] public string TerrainCustomDataName = "terrain";

    [Export] public int TerrainTileSetSourceId = -1;

    [Export] public Godot.Collections.Dictionary<Terrain, Vector2I> TerrainTileSetAtlasCoords = [];

    public readonly GridData Data = new();

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Data.Size = GroundLayer.GetUsedRect().End;
            Data.DefaultTerrain = DefaultTerrain;
            if (TerrainLayer is not null)
            {
                foreach (Vector2I cell in TerrainLayer.GetUsedCells())
                    Data.Terrain[cell] = TerrainLayer.GetCellTileData(cell).GetCustomData(TerrainCustomDataName).As<Terrain>();
                Data.TerrainUpdated += (cell, terrain) => {
                    if (terrain == DefaultTerrain)
                        TerrainLayer.SetCell(cell, -1, -Vector2I.One);
                    else
                        TerrainLayer.SetCell(cell, TerrainTileSetSourceId, TerrainTileSetAtlasCoords[terrain]);
                };
            }
        }
    }

    /// <summary>Grid cell dimensions derived from the <see cref="TileSet"/>.  If there is no <see cref="TileSet"/>, the size is zero.</summary>
    public Vector2 CellSize => GroundLayer?.TileSet?.TileSize ?? Vector2.Zero;

    public Vector2I Size => Engine.IsEditorHint() ? GroundLayer?.GetUsedRect().End ?? Vector2I.Zero : Data.Size;

    /// <summary>Characters and objects occupying the grid.</summary>
    public readonly Dictionary<Vector2I, GridNode> Occupants = [];

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

    public bool Contains(Vector2I cell) => Data.Contains(cell);
    public bool IsTraversable(Vector2I cell, Faction faction) => !Occupants.TryGetValue(cell, out GridNode occupant) || (occupant is Unit unit && unit.Faction.AlliedTo(faction));
    public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => Data.GetCellsAtDistance(cell, distance);
    public Terrain GetTerrain(Vector2I cell) => Data.Terrain.TryGetValue(cell, out Terrain terrain) ? terrain : Data.DefaultTerrain;
    public int PathCost(IEnumerable<Vector2I> path) => IGrid.PathCost(this, path);
    public IImmutableDictionary<Vector2I, Unit> GetOccupantUnits() => Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => e.Value as Unit);
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