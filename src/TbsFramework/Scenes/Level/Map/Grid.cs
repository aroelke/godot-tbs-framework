using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Data;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Map;

/// <summary>Defines the grid dimensions and attributes and contains the locations of the objects and terrain within it.</summary>
[Tool]
public partial class Grid : Node2D
{
    private readonly StringName TerrainCustomDataProperty = "TerrainCustomDataName";
    private const string TerrainCustomDataDefault = "terrain";

    private string _terrainCustomDataName = TerrainCustomDataDefault;
    private readonly Dictionary<Terrain, (int sourceId, Vector2I atlasCoords)> _terrainCoords = [];

    /// <summary><see cref="TileMapLayer"/> containing ground tiles.</summary>
    [Export] public TileMapLayer GroundLayer = null;

    /// <summary><see cref="TileMapLayer"/> layer containing terrain tiles. This is the layer that will be used to define terrain effects.</summary>
    [Export] public TileMapLayer TerrainLayer = null;

    /// <summary>Default terrain to use when it isn't placed explicitly on the map.</summary>
    [Export] public Terrain DefaultTerrain
    {
        get
        {
            if (!Engine.IsEditorHint() && IsNodeReady())
                GD.PushWarning("Use GridData.DefaultTerrain to get/set default terrain while the game is running.");
            return Data.DefaultTerrain;
        }
        set
        {
            if (!Engine.IsEditorHint() && IsNodeReady())
                GD.PushWarning("Use GridData.DefaultTerrain to get/set default terrain while the game is running.");
            Data.DefaultTerrain = value;
        }
    }

    /// <summary>Structure containing the state of the grid during gameplay.</summary>
    public readonly GridData Data = new();

    /// <summary>Grid cell dimensions derived from the <see cref="TileSet"/>.  If there is no <see cref="TileSet"/>, the size is zero.</summary>
    public Vector2 CellSize => GroundLayer?.TileSet?.TileSize ?? Vector2.Zero;

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

    public IEnumerable<Vector2I> GetCellsAtDistance(Vector2I cell, int distance) => Data.GetCellsAtDistance(cell, distance);
    public Terrain GetTerrain(Vector2I cell) => Data.Terrain.TryGetValue(cell, out Terrain terrain) ? terrain : Data.DefaultTerrain;
    public int PathCost(IEnumerable<Vector2I> path) => Data.PathCost(path);
    public IImmutableDictionary<Vector2I, Unit> GetOccupantUnits() => Occupants.Where((e) => e.Value is Unit).ToImmutableDictionary((e) => e.Key, (e) => e.Value as Unit);
    public IEnumerable<SpecialActionRegion> GetSpecialActionRegions() => SpecialActionRegions;

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

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = [.. base._GetPropertyList() ?? []];

        if (TerrainLayer?.TileSet is not null)
        {
            properties.Add(ObjectProperty.CreateEnumProperty(
                TerrainCustomDataProperty,
                Enumerable.Range(0, TerrainLayer.TileSet.GetCustomDataLayersCount()).Select(TerrainLayer.TileSet.GetCustomDataLayerName)
            ));
        }

        return properties;
    }

    public override Variant _Get(StringName property) => property == TerrainCustomDataProperty ? _terrainCustomDataName : base._Get(property);

    public override bool _Set(StringName property, Variant value)
    {
        if (property == TerrainCustomDataProperty)
        {
            _terrainCustomDataName = value.AsString();
            return true;
        }
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property) => property == TerrainCustomDataProperty || base._PropertyCanRevert(property);

    public override Variant _PropertyGetRevert(StringName property) => property == TerrainCustomDataProperty ? TerrainCustomDataDefault : base._PropertyGetRevert(property);

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Data.Size = GroundLayer.GetUsedRect().End;
            if (TerrainLayer is not null)
            {
                foreach (Vector2I cell in TerrainLayer.GetUsedCells())
                    Data.Terrain[cell] = TerrainLayer.GetCellTileData(cell).GetCustomData(_terrainCustomDataName).As<Terrain>();

                for (int i = 0; i < TerrainLayer.TileSet.GetSourceCount(); i++)
                {
                    int id = TerrainLayer.TileSet.GetSourceId(i);
                    if (TerrainLayer.TileSet.GetSource(id) is TileSetAtlasSource source)
                    {
                        for (int t = 0; t < source.GetTilesCount(); t++)
                        {
                            Vector2I atlas = source.GetTileId(t);
                            TileData data = source.GetTileData(atlas, 0);
                            Terrain terrain = data.GetCustomData(_terrainCustomDataName).As<Terrain>();
                            if (terrain is not null)
                                _terrainCoords[terrain] = (id, atlas);
                        }
                    }
                }

                Data.TerrainUpdated += (cell, _, terrain) => {
                    if (terrain == DefaultTerrain)
                        TerrainLayer.SetCell(cell, -1, -Vector2I.One);
                    else
                    {
                        (int id, Vector2I atlas) = _terrainCoords[terrain];
                        TerrainLayer.SetCell(cell, id, atlas);
                    }
                };
            }
        }
    }
}