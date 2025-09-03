using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

public abstract partial class AreaSwitchCondition : SwitchCondition
{
    [Export] public Unit[] TriggerUnits = [];

    [Export] public Army[] TriggerArmies = [];

    [Export] public bool Inside = true;

    [Export] public bool RequiresEveryone = false;

    public abstract HashSet<Vector2I> GetRegion();

    public IEnumerable<Unit> GetApplicableUnits()
    {
        List<Unit> applicable = [.. TriggerUnits];
        foreach (Army army in TriggerArmies)
            applicable.AddRange(army);
        return applicable;
    }

    public void Update(Unit unit)
    {
        if (!GetApplicableUnits().Any())
            return;

        HashSet<Vector2I> region = GetRegion();
        IEnumerable<Unit> applicable = GetApplicableUnits();
        Func<Func<Unit, bool>, bool> matcher = RequiresEveryone ? GetApplicableUnits().All : GetApplicableUnits().Any;
        Func<Unit, bool> container = Inside ? (u) => region.Contains(u.Cell) : (u) => !region.Contains(u.Cell);

        Satisfied = matcher(container);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (TriggerUnits.Length == 0 && TriggerArmies.Length == 0)
            warnings.Add("This condition doesn't apply to any units and will never be satisfied.");

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