using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Events;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class TurnSwitchCondition : SwitchCondition
{
    [Export(PropertyHint.Expression, "1,10,or_greater")] public int TriggerTurn = 1;

    public void Update(int turn, Army army)
    {
        if (TriggerArmies.Contains(army))
            Satisfied = turn >= TriggerTurn;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.TurnBegan += Update;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.TurnBegan -= Update;
    }

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].AsStringName() == PropertyName.TriggerUnits)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}