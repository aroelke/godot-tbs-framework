using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Properties;

namespace TbsFramework.Scenes.Rendering;

/// <summary>
/// Specialized <see cref="TileMapLayer"/> that saves <see cref="TileSet"/> information required for directly setting the used cells. Supports
/// either using a single defined terraing from the <see cref="TileSet"/> or filling the cells with repetitions of a single tile.
/// </summary>
[GlobalClass, Tool]
public partial class HighlightedRegion : TileMapLayer
{
    private static readonly StringName TerrainSet = "Terrain Set";
    private static readonly StringName Terrain = "Terrain";
    private static readonly StringName SourceId = "Source ID";
    private static readonly StringName AtlasCoords = "Atlas Coordinates";

    private int _set = 0;
    private int _terrain = 0;
    private int _id = 0;
    private Vector2I _coords = Vector2I.Zero;

    /// <summary>Set the used cells for the layer.</summary>
    /// <remarks>
    /// <b>Warning</b>: This will clear all used cells from the layer, including ones that aren't part of the selected terrain
    /// or aren't the selected tile.
    /// </remarks>
    public void SetUsedCells(IEnumerable<Vector2I> cells)
    {
        if (TileSet is null)
            return;

        Clear();
        if (TileSet.GetTerrainSetsCount() > _set && TileSet.GetTerrainsCount(_set) > _terrain)
            SetCellsTerrainConnect([.. cells], _set, _terrain);
        else
            foreach (Vector2I cell in cells)
                SetCell(cell, _id, _coords);
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = [.. base._GetPropertyList() ?? []];

        if (TileSet is not null)
        {
            if (TileSet.GetTerrainSetsCount() > 0)
            {
                // Set the terrain set to use for occupancy determination
                properties.Add(new ObjectProperty(
                    TerrainSet,
                    Variant.Type.Int,
                    Hint:PropertyHint.Range,
                    HintString:$"0,{TileSet.GetTerrainSetsCount() - 1}",
                    Usage:[PropertyUsageFlags.Default, TileSet.GetTerrainSetsCount() == 1 ? PropertyUsageFlags.ReadOnly : 0]
                ));
                if (TileSet.GetTerrainsCount(_set) > 0)
                {
                    // Set the terrain within the set to use for occupancy determination
                    properties.Add(new ObjectProperty(
                        Terrain,
                        Variant.Type.String,
                        Hint:PropertyHint.Enum,
                        HintString:string.Join(',', Enumerable.Range(0, TileSet.GetTerrainsCount(_set)).Select((i) => TileSet.GetTerrainName(_set, i))),
                        Usage:[PropertyUsageFlags.Default, TileSet.GetTerrainsCount(_set) == 1 ? PropertyUsageFlags.ReadOnly : 0]
                    ));
                }
            }
            else
            {
                if (TileSet.GetSourceCount() > 0)
                {
                    properties.Add(new ObjectProperty(
                        SourceId,
                        Variant.Type.Int,
                        Hint:PropertyHint.Enum,
                        HintString:string.Join(',', Enumerable.Range(0, TileSet.GetSourceCount()).Select(TileSet.GetSourceId)),
                        Usage:[PropertyUsageFlags.Default, TileSet.GetSourceCount() == 1 ? PropertyUsageFlags.ReadOnly : 0]
                    ));
                }
                properties.Add(new ObjectProperty(AtlasCoords, Variant.Type.Vector2I));
            }
        }

        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (property == TerrainSet)
            return _set;
        else if (property == Terrain)
            return TileSet?.GetTerrainName(_set, _terrain) ?? "";
        else if (property == SourceId)
            return _id;
        else if (property == AtlasCoords)
            return _coords;
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == TerrainSet)
        {
            _set = value.AsInt32();
            return true;
        }
        else if (property == Terrain)
        {
            _terrain = 0;
            if (TileSet is not null)
            {
                string name = value.AsString();
                for (int i = 0; i < TileSet.GetTerrainsCount(_set); i++)
                    if (TileSet.GetTerrainName(_set, i) == name)
                        _terrain = i;
            }
            return true;
        }
        else if (property == SourceId)
        {
            _id = value.AsInt32();
            return true;
        }
        else if (property == AtlasCoords)
        {
            _coords = value.AsVector2I();
            return true;
        }
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property) =>
        property == TerrainSet  ||
        property == Terrain     ||
        property == SourceId    ||
        property == AtlasCoords ||
        base._PropertyCanRevert(property);

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == TerrainSet)
            return 0;
        else if (property == Terrain)
            return TileSet?.GetTerrainName(_set, 0) ?? "";
        else if (property == SourceId)
            return TileSet?.GetSourceId(0) ?? 0;
        else if (property == AtlasCoords)
            return Vector2I.Zero;
        else
            return base._PropertyGetRevert(property);
    }
}