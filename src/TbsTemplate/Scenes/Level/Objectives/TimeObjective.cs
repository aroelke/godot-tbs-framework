using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that is completed after a certain number of turns from the beginning of the level have elapsed.</summary>
[Tool]
public partial class TimeObjective : Objective
{
    private int _turn = 0;

    /// <summary>Number of turns to elapse before completion.</summary>
    [Export(PropertyHint.Range, "1,10,or_greater")] public int Turns = 0;

    public override bool Complete => _turn > Turns;
    public override string Description => $"Survive {Turns} Turns";

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.Connect<int, Army>(LevelEvents.SignalName.TurnBegan, (t, _) => _turn = t);
    }
}