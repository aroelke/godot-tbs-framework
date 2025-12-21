using Godot;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Level.Object.Group;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Behavior switch condition that triggers when a particular turn is reached.</summary>
[Tool]
public partial class TurnSwitchCondition : SwitchCondition
{
    /// <summary>Turn that triggers the behavior switch.</summary>
    [Export(PropertyHint.Expression, "1,10,or_greater")] public int TriggerTurn = 1;

    /// <summary>Army whose turn should be tracked for triggering. Leave <c>null</c> to track all armies.</summary>
    [Export] public Army TriggerArmy = null;

    public void Update(int turn, Army army)
    {
        if (TriggerArmy is null || army == TriggerArmy)
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
}