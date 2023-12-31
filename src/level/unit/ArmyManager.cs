using Godot;
using level.manager;
using level.Object;
using System;
using System.Collections.Generic;
using System.Linq;

namespace level.unit;

/// <summary>Manages a group of allied units.  Can optionally be allied with units in other armies as well.</summary>
public partial class ArmyManager : Node
{
    private LevelManager _levelManager = null;

    public LevelManager LevelManager => _levelManager ??= GetParent<LevelManager>();

    /// <summary>Other armies that are allied to this one.</summary>
    [Export] public ArmyManager[] Allies = Array.Empty<ArmyManager>();

    /// <summary>Units within this army and their locations on the map.</summary>
    public readonly Dictionary<Vector2I, Unit> Units = new();

    /// <param name="unit">Unit to check.</param>
    /// <returns><c>true</c> if this army contains the unit, and <c>false</c> otherwise.</returns>
    public bool Contains(Unit unit) => Units.ContainsValue(unit);

    /// <param name="other">Army to check.</param>
    /// <returns><c>true</c> if the other army is allied with this one, and <c>false</c> otherwise.</returns>
    public bool AlliedTo(ArmyManager other) => other == this || Allies.Contains(other);

    /// <param name="unit">Unit to check.</param>
    /// <returns><c>true</c> if the unit is in this army or an allied one, and <c>false</c> otherwise.</returns>
//    public bool AlliedTo(Unit unit) => Contains(unit) || AlliedTo(unit.Affiliation);

    public override void _Ready()
    {
        base._Ready();
        foreach (Node child in GetChildren())
        {
            if (child is Unit unit)
            {
                Units[unit.Cell] = unit;
//                unit.Affiliation = this;
            }
        }
    }
}