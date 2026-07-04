using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

/// <summary>Grants permission to specific units to perform an action.</summary>
[GlobalClass, Tool]
public partial class ActionPermissionUnit : ActionPermission
{
    /// <summary>List of node paths in the current scene to the units to allow relative to the parameter of <see cref="Initialize"/></summary>
    [Export] public NodePath[] AllowedUnitPaths = [];

    /// <summary>
    /// Identities of units resolved from <see cref="AllowedUnitPaths"/> after <see cref="Initialize"/> completes. If empty, any unit can perform
    /// the action.
    /// </summary>
    public IEnumerable<UnitReferenceType> AllowedUnitIdentities = [];

    public override bool CanPerform(UnitData unit) => !AllowedUnitIdentities.Any() || AllowedUnitIdentities.Any((u) => u == unit.Identity);
    public override void Initialize(LevelManager manager) => AllowedUnitIdentities = AllowedUnitPaths.Select((p) => manager.GetNode<Unit>(p).UnitData.Identity);
}