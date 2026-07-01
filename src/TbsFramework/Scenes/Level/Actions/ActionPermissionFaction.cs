using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>Gives permission to a unit to perform an action based on the <see cref="Faction"/> it belongs to.</summary>
[GlobalClass, Tool]
public partial class ActionPermissionFaction : ActionPermission
{
    /// <summary>List of factions allowed to perform an action. Leave empty to allow all factions to perform it.</summary>
    [Export] public Faction[] AllowedFactions = [];

    public override bool CanPerform(UnitData unit) => AllowedFactions.Length == 0 || AllowedFactions.Contains(unit.Faction);
}