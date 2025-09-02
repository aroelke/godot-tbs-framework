using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

public abstract partial class AreaSwitchCondition : SwitchCondition
{
    [Export] public bool Inside = true;

    [Export] public bool RequiresEveryone = false;

    public abstract HashSet<Vector2I> GetRegion();

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