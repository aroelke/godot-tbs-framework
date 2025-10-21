using System;
using Godot;

namespace TbsTemplate.Nodes.StateCharts.States;

[Tool]
public partial class HistoryState : State
{
    public StateRecord History;

    [Export] public State DefaultState = null;

    public override void Enter(bool transit = false) => throw new InvalidOperationException("History states can't be entered.");
    public override StateRecord SaveHistory() => History;
    public override void RestoreHistory(StateRecord record) => History = record;
    public override void HandleTransition(StateTransition transition, State from) => throw new InvalidOperationException("History states can't be transitioned from.");
}