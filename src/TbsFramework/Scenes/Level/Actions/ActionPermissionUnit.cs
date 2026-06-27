using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass, Tool]
public partial class ActionPermissionUnit : ActionPermission
{
    [Export] public NodePath[] AllowedUnitPaths = [];

    public IEnumerable<UnitData> AllowedUnits = [];

    public override bool CanPerform(UnitData unit) => !AllowedUnits.Any() || AllowedUnits.Contains(unit);

    public override void Initialize(LevelManager manager) => AllowedUnits = AllowedUnitPaths.Select((p) => manager.GetNode<Unit>(p).UnitData);
}