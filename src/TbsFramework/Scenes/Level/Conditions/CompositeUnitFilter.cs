using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Represents a collection of units that could be from multiple factions or be multiple specific units.</summary>
[GlobalClass, Tool]
public partial class CompositeUnitFilter : UnitFilter
{
    /// <summary>Specific units that should be part of the collection.</summary>
    [Export] public UnitIdentity[] Units = [];

    /// <summary>Factions containing units that should be part of the collection.</summary>
    [Export] public Faction[] Factions = [];

    public override bool IsIncluded(UnitData unit) => Units.Contains(unit.Identity) || Factions.Contains(unit.Faction);
}