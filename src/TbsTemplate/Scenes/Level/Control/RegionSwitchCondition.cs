using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class RegionSwitchCondition : SwitchCondition
{
    [Export] public TileMapLayer TriggerRegion = null;

    [Export] public bool Inside = true;

    [Export] public bool RequiresEveryone = false;

    public void Update(Unit unit)
    {
        if (TriggerRegion is null || !GetApplicableUnits().Any())
            return;

        Godot.Collections.Array<Vector2I> region = TriggerRegion.GetUsedCells();
        Func<Func<Unit, bool>, bool> matcher = RequiresEveryone ? GetApplicableUnits().All : GetApplicableUnits().Any;
        Func<Unit, bool> container = Inside ? (u) => region.Contains(u.Cell) : (u) => !region.Contains(u.Cell);

        Satisfied = matcher(container);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (TriggerRegion is null || TriggerRegion.GetUsedCells().Count == 0)
            warnings.Add("No trigger region is defined. Behavior switch will never occur.");

        return [.. warnings];
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.ActionEnded += Update;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.ActionEnded -= Update;
    }
}