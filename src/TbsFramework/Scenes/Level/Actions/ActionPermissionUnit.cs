using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>Grants permission to specific units to perform an action.</summary>
[GlobalClass, Tool]
public partial class ActionPermissionUnit : ActionPermission
{
    /// <summary>
    /// Identities of units resolved from <see cref="AllowedUnitPaths"/> after <see cref="Initialize"/> completes. If empty, any unit can perform
    /// the action.
    /// </summary>
    [Export] public UnitIdentity[] AllowedUnitIdentities = [];

    public override bool CanPerform(UnitData unit) => AllowedUnitIdentities.Length == 0 || AllowedUnitIdentities.Any((u) => u == unit.Identity);
    public override void Initialize(Node owner) {}
}