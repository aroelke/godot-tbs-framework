using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Scenes.Level.Layers;

[Tool]
public partial class PathLayer : TileMapLayer
{
    // TileSet source ID for the path arrows and indices containing arrowheads.
    // XXX: DEPENDS STRONGLY ON TILESET ORGANIZATION
    private const int PathSourceId = 3;
    private static readonly Vector2I RightArrow = new(6, 0);
    private static readonly Vector2I DownArrow = new(7, 0);
    private static readonly Vector2I UpArrow = new(6, 1);
    private static readonly Vector2I LeftArrow = new(7, 1);

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
                SetCellsTerrainPath([.. value], 0, 0);
                SetCell(value[^1], sourceId:PathSourceId, atlasCoords:(value[^1] - value[^2]) switch
                {
                    Vector2I(0, >0) => DownArrow,
                    Vector2I(>0, 0) => RightArrow,
                    Vector2I(0, <0) => UpArrow,
                    Vector2I(<0, 0) => LeftArrow,
                    _ => new(8, 0)
                });
            }
        }
    }
}