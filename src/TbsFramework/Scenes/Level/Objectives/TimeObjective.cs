using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Objectives;

/// <summary>Objective that is completed after a certain number of turns from the beginning of the level have elapsed.</summary>
[Tool]
public partial class TimeObjective : Objective
{
    private int _turn = 0;

    private void OnTurnBegan(int turn, Faction _) => _turn = turn;

    /// <summary>Number of turns to elapse before completion.</summary>
    [Export(PropertyHint.Range, "1,10,or_greater")] public int Turns = 0;

    public override bool Complete => _turn > Turns;
    public override string Description => $"Survive {Turns} Turns";

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
            LevelEvents.TurnBegan += OnTurnBegan;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
            LevelEvents.TurnBegan -= OnTurnBegan;
    }
}