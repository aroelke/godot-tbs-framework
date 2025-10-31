using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Scenes.Level.Layers;

/// <summary>Special <see cref="TileMapLayer"/> designed to draw arrows for paths across a map.</summary>
[Tool]
public partial class PathLayer : TileMapLayer
{
    /// <summary>Source ID within the tile set containing the arrow tiles.</summary>
    [Export] public int PathSourceId = -1;

    /// <summary>ID of the terrain set in the tile set to use for connecting path tiles.</summary>
    [Export] public int PathTerrainSet = -1;

    /// <summary>ID of the terrain within the terrain set of the tile set to use for connecting path tiles.</summary>
    [Export] public int PathTerrain = -1;

    /// <summary>Tile set atlas coordinates of the up arrowhead.</summary>
    [Export] public Vector2I UpArrowCoordinates = -Vector2I.One;

    /// <summary>Tile set atlas coordinates of the right arrowhead.</summary>
    [Export] public Vector2I RightArrowCoordinates = -Vector2I.One;

    /// <summary>Tile set atlas coordinates of the down arrowhead.</summary>
    [Export] public Vector2I DownArrowCoordinates = -Vector2I.One;

    /// <summary>Tile set atlas coordinates of the left arrowhead.</summary>
    [Export] public Vector2I LeftArrowCoordinates = -Vector2I.One;

    /// <summary>List of cells defining the path to draw.</summary>
    /// <remarks>Is not a <see cref="Map.Path"/> to decouple from <see cref="Map.Grid"/>.</remarks>
    public List<Vector2I> Path
    {
        get => [.. GetUsedCells()];
        set
        {
            Clear();
            if (value.Count > 1)
            {
                SetCellsTerrainPath([.. value], PathTerrainSet, PathTerrain);
                Vector2I coordinates = (value[^1] - value[^2]) switch
                {
                    Vector2I(0, >0) => DownArrowCoordinates,
                    Vector2I(>0, 0) => RightArrowCoordinates,
                    Vector2I(0, <0) => UpArrowCoordinates,
                    Vector2I(<0, 0) => LeftArrowCoordinates,
                    _ => new(8, 0)
                };
                if (coordinates != -Vector2I.One)
                    SetCell(value[^1], sourceId:PathSourceId, atlasCoords:coordinates);
            }
        }
    }
}