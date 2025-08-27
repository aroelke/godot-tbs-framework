using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class InRangeSwitchCondition : SwitchCondition
{
    public void Update(Unit unit)
    {
        IEnumerable<Unit> applicable = GetApplicableUnits();

        if (applicable.Contains(unit))
        {
            HashSet<Vector2I> region = [.. applicable.SelectMany(static (u) => u.AttackableCells())];
            IEnumerable<Unit> opposing = unit.Grid.Occupants.Values.OfType<Unit>().Where(applicable.Contains);
            Satisfied = opposing.Any((u) => region.Contains(u.Cell));
        }
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