using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass, Tool]
public partial class ActionPermissionFaction : ActionPermission
{
    [Export] public Faction[] AllowedFactions = [];

    public override bool CanPerform(UnitData unit) => AllowedFactions.Length == 0 || AllowedFactions.Contains(unit.Faction);
}