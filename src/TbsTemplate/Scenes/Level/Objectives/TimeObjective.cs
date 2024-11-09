using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that is completed after a certain number of turns from the beginning of the level have elapsed.</summary>
[Tool]
public partial class TimeObjective : Objective
{
    private int _turn = 0;

    /// <summary>Number of turns to elapse before completion.</summary>
    [Export] public int Turns = 0;

    public override bool Complete => _turn > Turns;
    public override string Description => $"Survive {Turns} Turns";

    public void OnTurnBegan(int turn) => _turn = turn;

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Turns < 0)
            warnings.Add("Turn count should not be negative.");
        else if (Turns == 0)
            warnings.Add("Time objective will always be completed.");

        return [.. warnings];
    }
}