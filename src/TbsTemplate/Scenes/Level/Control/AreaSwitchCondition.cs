using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Control;

public abstract partial class AreaSwitchCondition : SwitchCondition
{
    public abstract void Update(Unit unit);

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