using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Object.Group;

namespace TbsFramework.Scenes.Level.Layers;

/// <summary>Map layer marking out a region where a unit can perform a special action such as capture or escape.</summary>
[GlobalClass, Tool]
public partial class SpecialActionRegion : TileMapLayer
{
    private static readonly StringName TerrainSet = "Terrain Set";
    private static readonly StringName Terrain = "Terrain";

    private int _set = 0;
    private int _terrain = 0;

    /// <summary>Signifies that the action corresponding to the region has been performed.</summary>
    /// <param name="name">Name of the action being performed.</param>
    /// <param name="performer"><see cref="Unit"/> performing the action.</param>
    /// <param name="cell">Cell in which the unit performed the action.</param>
    [Signal] public delegate void SpecialActionPerformedEventHandler(StringName name, Unit performer, Vector2I cell);

    /// <summary>Short description of the action being performed for display in the UI (for example, in a <see cref="ContextMenu"/>).</summary>
    [Export] public StringName Action { get; set; } = "";

    /// <summary>List of armies whose units are allowed to perform the action.</summary>
    [Export] public Army[] AllowedArmies = [];

    /// <summary>List of individual units who are allowed to perform the action.</summary>
    [Export] public Unit[] AllowedUnits = [];

    /// <summary>Whether or not an action should remove the cell it's performed in from the region.</summary>
    [Export] public bool OneShot = false;

    /// <summary>Whether or not an action should only be performed once per unit.</summary>
    [Export] public bool SingleUse = false;

    /// <summary>Structure defining the state of the special action region.</summary>
    public readonly SpecialActionRegionData Data = new();

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = [.. base._GetPropertyList() ?? []];

        if (TileSet is not null && TileSet.GetTerrainSetsCount() > 0)
        {
            // Set the terrain set to use for occupancy determination
            properties.Add(new ObjectProperty(
                TerrainSet,
                Variant.Type.Int,
                PropertyHint.Range,
                $"0,{TileSet.GetTerrainSetsCount() - 1}"
            ));
            // Set the terrain within the set to use for occupancy determination
            properties.Add(new ObjectProperty(
                Terrain,
                Variant.Type.String,
                PropertyHint.Enum,
                string.Join(",", Enumerable.Range(0, TileSet.GetTerrainsCount(_set)).Select((i) => TileSet.GetTerrainName(_set, i)))
            ));
        }

        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (property == TerrainSet)
            return _set;
        else if (property == Terrain)
            return TileSet?.GetTerrainName(_set, _terrain) ?? "";
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
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property) => property == TerrainSet || property == Terrain || base._PropertyCanRevert(property);

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == TerrainSet)
            return 0;
        else if (property == Terrain)
            return TileSet?.GetTerrainName(_set, 0) ?? "";
        else
            return base._PropertyGetRevert(property);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (AllowedArmies.Length == 0 && AllowedUnits.Length == 0)
            warnings.Add("No units are allowed to perform the action. Action cannot be performed.");
        if (GetUsedCells().Count == 0)
            warnings.Add("No cells in region. Action cannot be performed.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        Data.Action = Action;
        Data.Cells = [.. GetUsedCells()];
        foreach (Army army in AllowedArmies)
            Data.AllowedFactions.Add(army.Faction);
        foreach (Unit unit in AllowedUnits)
            Data.AllowedUnits.Add(unit.UnitData);
        Data.OneShot = OneShot;
        Data.SingleUse = SingleUse;

        Data.CellsUpdated += (cells) => {
            Clear();
            if (cells.Count > 0)
                SetCellsTerrainConnect([.. cells], _set, _terrain);
        };
    }
}