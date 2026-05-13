using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Objectives;

/// <summary>Objective that is completed after a certain number of turns from the beginning of the level have elapsed.</summary>
[Tool]
public partial class TimeObjective : Objective
{
    /// <summary>Possible turn events that can trigger the time objective to update and potentially become completed.</summary>
    public enum TriggerEvent { TurnBegan, TurnEnded }

    private int _turn = 0;

    /// <summary>Number of turns to elapse before completion.</summary>
    [Export(PropertyHint.Range, "1,10,or_greater")] public int Turns = 0;

    /// <summary>Army whose turn count is to be tracked. Leave <c>null</c> to track all armies.</summary>
    [Export] public Army Army = null;

    /// <summary>When to update the turn counter.</summary>
    [Export] public TriggerEvent Trigger = TriggerEvent.TurnBegan;

    public override bool Complete => Trigger switch {
        TriggerEvent.TurnBegan => _turn > Turns,
        TriggerEvent.TurnEnded => _turn >= Turns,
        _ => false
    };
    public override string Description => $"Survive {Turns} Turns";

    /// <summary>Update the turn count if it's the right event for the right army's turn.</summary>
    /// <param name="turn">Turn to update to.</param>
    /// <param name="faction">Faction whose turn it should be to update.</param>
    /// <param name="trigger">Turn event to update the turn count on.</param>
    public void UpdateTurnCount(int turn, Faction faction, TriggerEvent trigger)
    {
        if (Trigger == trigger && (Army is null || Army.Faction == faction))
            _turn = turn;
    }

    public void OnTurnBegan(int turn, Faction faction) => UpdateTurnCount(turn, faction, TriggerEvent.TurnBegan);
    public void OnTurnEnded(int turn, Faction faction) => UpdateTurnCount(turn, faction, TriggerEvent.TurnEnded);

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint())
        {
            LevelEvents.TurnBegan += OnTurnBegan;
            LevelEvents.TurnEnded += OnTurnEnded;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint())
        {
            LevelEvents.TurnBegan -= OnTurnBegan;
            LevelEvents.TurnEnded -= OnTurnEnded;
        }
    }
}