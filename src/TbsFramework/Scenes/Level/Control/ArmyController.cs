using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using TbsFramework.Data;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object;
using TbsFramework.Scenes.Level.Object.Group;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Controller for determining which units act in a turn and how they act.</summary>
public abstract partial class ArmyController : Node
{
    /// <summary>Signals that the set of usable inputs that this controller can react to has changed. Mostly useful for updating the UI.</summary>
    /// <param name="actions"></param>
    [Signal] public delegate void EnabledInputActionsUpdatedEventHandler(StringName[] actions);

    /// <summary>Signals that a selection has been canceled.</summary>
    [Signal] public delegate void SelectionCanceledEventHandler();

    /// <summary>
    /// <para>Fast-forward through the rest of the current army's turn.</para>
    /// <para>For a player army, this can be called after deactivating all units to signal an early end of the turn.</para>
    /// <para>For an AI army, this can be used to skip animating the AI's turn.</para>
    /// </summary>
    [Signal] public delegate void TurnFastForwardEventHandler();

    /// <summary>Signals that a unit has been chosen to act.</summary>
    /// <param name="cell">Cell containing the selected unit.</param>
    [Signal] public delegate void UnitSelectedEventHandler(Vector2I cell);

    /// <summary>Signals that a path for a unit to move on has been chosen.</summary>
    /// <param name="cell">Cell containing the unit that will move along the path.</param>
    /// <param name="path">Contiguous list of cells for the unit to move through.</param>
    [Signal] public delegate void PathConfirmedEventHandler(Vector2I cell, Godot.Collections.Array<Vector2I> path);
    /// <summary>Signals that an action has been chosen for a unit.</summary>
    /// <param name="cell">Cell containing the unit being commanded.</param>
    /// <param name="command">String representing the action to perform.</param>
    [Signal] public delegate void UnitCommandedEventHandler(Vector2I cell, StringName command);

    /// <summary>Signals that a target for an action has been chosen.</summary>
    /// <param name="source">Cell containing the unit performing the action.</param>
    /// <param name="target">Target cell of the action.</param>
    [Signal] public delegate void TargetChosenEventHandler(Vector2I source, Vector2I target);

    /// <summary>Signals that targeting for a unit's action was canceled.</summary>
    /// <param name="source">Cell containing the unit whose action was canceled.</param>
    [Signal] public delegate void TargetCanceledEventHandler(Vector2I source);

    /// <summary>Signals that a unit's action has been finalized and the current army's turn has progressed.</summary>
    /// <param name="completed">Number of units that have completed actions, including the one that just did.</param>
    /// <param name="remaining">Number of units that can still act.</param>
    [Signal] public delegate void ProgressUpdatedEventHandler(int completed, int remaining);

    private Army _army = null;
    private readonly ImmutableDictionary<StringName, List<Callable>> _turnSignals;

    /// <summary>Army being controlled. Should be the direct parent of this controller.</summary>
    public Army Army => _army ??= GetParentOrNull<Army>();

    public ArmyController() : base()
    {
        Dictionary<StringName, List<Callable>> signals = [];
        signals[SignalName.SelectionCanceled] = [];
        signals[SignalName.TurnFastForward] = [];
        signals[SignalName.UnitSelected] = [];
        signals[SignalName.PathConfirmed] = [];
        signals[SignalName.UnitCommanded] = [];
        signals[SignalName.TargetChosen] = [];
        signals[SignalName.TargetCanceled] = [];
        _turnSignals = signals.ToImmutableDictionary();
    }

    /// <summary>Grid that the army's units will be acting on.</summary>
    [Export] public abstract Grid Grid { get; set; }

    /// <summary>Connect a signal only for the duration of the army's turn.</summary>
    /// <param name="signal">Name of the signal to connect.</param>
    /// <param name="callable">Function to perform when the signal is raised.</param>
    public void ConnectForTurn(StringName signal, Callable callable)
    {
        Connect(signal, callable);
        _turnSignals[signal].Add(callable);
    }

    /// <summary>Perform any setup needed to begin the army's turn.</summary>
    public abstract void InitializeTurn();

    /// <summary>Skip through the rest of this army's turn.</summary>
    public abstract void FastForwardTurn();

    /// <summary>Choose a unit in the army to select. Once the unit has been selected, emit <c>UnitSelected</c>.</summary>
    public abstract void SelectUnit();

    /// <summary>Choose the path along which a unit will move. Once the path has been determined, emit <c>UnitMoved</c>.</summary>
    /// <param name="unit">Unit to move.</param>
    public abstract void MoveUnit(UnitData unit);

    /// <summary>Choose an action for a unit to perform. Once a command has been selected, emit <c>UnitCommanded</c>.</summary>
    /// <param name="source">Unit chosen to perform a command.</param>
    /// <param name="commands">List of commands available to perform.</param>
    /// <param name="cancel">Command to perform on cancel.</param>
    public abstract void CommandUnit(UnitData source, Godot.Collections.Array<StringName> commands, StringName cancel);

    /// <summary>Choose the target for an action that was selected.</summary>
    /// <param name="source">Unit that will perform the action.</param>
    /// <param name="targets">Cells <paramref name="source"/> can act on.</param>
    public abstract void SelectTarget(UnitData source, IEnumerable<Vector2I> targets);

    /// <summary>Clean up at the end of a unit's action and get ready for the next unit's action.</summary>
    public abstract void FinalizeAction();

    /// <summary>
    /// <para>Clean up at the end of an army's turn. Disconnects signals connected using <see cref="ConnectForTurn"/>.</para>
    /// <para><b>Note</b>: Make sure to call this from overriding functions, or the disconnection won't happen.</para>
    /// </summary>
    public virtual void FinalizeTurn()
    {
        foreach ((StringName signal, List<Callable> callables) in _turnSignals)
        {
            foreach (Callable callable in callables)
                Disconnect(signal, callable);
            callables.Clear();
        }

        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (GetParentOrNull<Army>() is null)
            warnings.Add("This controller does not belong to an army. It has nothing to control.");

        return [.. warnings];
    }
}