using Godot;
using System.Collections.Generic;

namespace battle;

/// <summary>Represents the battle map, containing its terrain and managing units and obstacles on it.</summary>
[Tool]
public partial class BattleMap : TileMap
{
    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    [Export] public Vector2I Size { get; private set; } = Vector2I.Zero;

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        if (Size.X <= 0 || Size.Y <= 0)
            warnings.Add($"Grid size {Size} has illegal dimensions.");
        return warnings.ToArray();
    }
}
