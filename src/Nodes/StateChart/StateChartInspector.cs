using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Nodes.StateChart.States;

namespace Nodes.StateChart;

/// <summary>
/// UI element that displays the current <see cref="State"/> and <see cref="State"/>, <see cref="Transition"/>, and event history of a
/// <see cref="Chart"/>.
/// </summary>
[Icon("res://icons/statechart/StateChartInspector.svg"), SceneTree]
public partial class StateChartInspector : MarginContainer
{
    /// <summary>Stores the <see cref="Chart"/> history and converts <see cref="State"/> changes, <see cref="Transition"/>s, and events into strings.</summary>
    private class History
    {
        private ImmutableList<string> _buffer = [];

        /// <summary>Whether or not the history has changed since the last UI update.</summary>
        public bool Dirty { get; private set; } = false;

        /// <summary>Maximum number of lines to show in the history.</summary>
        public int MaximumLines = 300;

        public void AddHistoryEntry(ulong frame, string text)
        {
            _buffer = _buffer.Add($"[{frame}]: {text}\n").TakeLast(MaximumLines).ToImmutableList();
            Dirty = true;
        }

        public void AddTransition(ulong frame, string name, string from, string to) => AddHistoryEntry(frame, $"[»] Transition: {name} from {from} to {to}");
        public void AddEvent(ulong frame, StringName @event) => AddHistoryEntry(frame, $"[!] Event received: {@event}");
        public void AddStateEntered(ulong frame, StringName name) => AddHistoryEntry(frame, $"[>] Enter: {name}");
        public void AddStateExited(ulong frame, StringName name) => AddHistoryEntry(frame, $"[<] Exit: {name}");

        public void Clear()
        {
            _buffer = _buffer.Clear();
            Dirty = true;
        }

        public string GetHistoryText()
        {
            Dirty = false;
            return string.Join("", _buffer);
        }
    }

    private readonly Dictionary<State, (State.StateEnteredEventHandler, State.StateExitedEventHandler)> _connectedStates = [];
    private readonly Dictionary<Transition, Transition.TakenEventHandler> _connectedTransitions = [];
    private History _history = null;

    /// <summary>
    /// Connect all of the <see cref="Chart.EventReceived"/> <see cref="State.StateEntered"/>, <see cref="State.StateExited"/>, and <see cref="Transition.Taken"/>
    /// signals to handlers that update the UI.
    /// </summary>
    private void ConnectAllSignals()
    {
        _connectedStates.Clear();
        _connectedTransitions.Clear();

        if (!IsInstanceValid(StateChart))
            return;
        
        void ConnectSignals(State state)
        {
            void entered() => OnStateEntered(state);
            void exited() => OnStateExited(state);
            state.StateEntered += entered;
            state.StateExited += exited;
            _connectedStates[state] = (entered, exited);

            foreach (Node child in state.GetChildren())
            {
                if (child is State substate)
                    ConnectSignals(substate);
                else if (child is Transition transition)
                {
                    void taken() => OnBeforeTransition(transition, state);
                    transition.Taken += taken;
                    _connectedTransitions[transition] = taken;
                }
            }
        }

        StateChart.EventReceived += OnEventReceived;
        foreach (State state in StateChart.GetChildren().OfType<State>())
            ConnectSignals(state);
    }

    /// <summary>Disconnect all signals from the <see cref="Chart"/> so they can be reconnected to another chart later.</summary>
    private void DisconnectAllSignals()
    {
        if (IsInstanceValid(StateChart))
            if (!IgnoreEvents)
                StateChart.EventReceived -= OnEventReceived;

        foreach ((State state, (State.StateEnteredEventHandler entered, State.StateExitedEventHandler exited)) in _connectedStates)
        {
            if (IsInstanceValid(state))
            {
                state.StateEntered -= entered;
                state.StateExited -= exited;
            }
        }

        foreach ((Transition transition, Transition.TakenEventHandler action) in _connectedTransitions)
            if (IsInstanceValid(transition))
                transition.Taken -= action;
    }

    private void SetupProcessing(bool a)
    {
        ProcessMode = Enabled ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;
        Visible = Enabled;
    }

    /// <summary>Find the active <see cref="State"/>s in the <see cref="Chart"/> and create a <see cref="Godot.Tree"/> representing them.</summary>
    /// <param name="root">Node to find child <see cref="State"/>s in to add to the <see cref="Godot.Tree"/>.</param>
    /// <param name="parent">Item in the <see cref="Godot.Tree"/> to add to.</param>
    private void CollectActiveStates(Node root, TreeItem parent)
    {
        foreach (State state in root.GetChildren().OfType<State>())
        {
            if (state.Active)
            {
                TreeItem item = Tree.CreateItem(parent);
                item.SetText(0, state.Name);
                CollectActiveStates(state, item);
            }
        }
    }

    private void ClearHistory()
    {
        HistoryEdit.Text = "";
        _history.Clear();
    }

    /// <summary>Whether or not to show the state of a <see cref="Chart"/> in the UI.</summary>
    [Export] public bool Enabled = true;

    /// <summary>State chart whose state is to be tracked.</summary>
    [Export] public Chart StateChart = null;

    /// <summary>Maximum number of <see cref="Chart"/> history lines to show.</summary>
    [Export] public int MaximumLines = 300;

    /// <summary>Don't show events sent to the <see cref="Chart"/> in the history.</summary>
    [Export] public bool IgnoreEvents = false;

    /// <summary>Don't show <see cref="State"/> changes in the <see cref="Chart"/> in the history.</summary>
    [Export] public bool IgnoreStateChanges = false;

    /// <summary>Don't show <see cref="State"/> in the <see cref="Chart"/> in the history.</summary>
    [Export] public bool IgnoreTransitions = false;

    /// <summary>Change the <see cref="Chart"/> to inspect in the UI.</summary>
    /// <param name="chart">New chart to watch.</param>
    public void InspectChart(Chart chart)
    {
        if (Enabled)
        {
            DisconnectAllSignals();
            StateChart = chart;
            if (StateChart is null)
            {
                GD.PushWarning("No state chart specified. Disabling inspector.");
                SetupProcessing(false);
            }
            else
            {
                ConnectAllSignals();
                ClearHistory();
                SetupProcessing(true);
            }
        }
    }

    /// <summary>Add an event to the history.</summary>
    /// <param name="event">Name of the event to add.</param>
    public void OnEventReceived(StringName @event)
    {
        if (!IgnoreEvents)
            _history.AddEvent(Engine.GetProcessFrames(), @event);
    }

    /// <summary>Add a <see cref="State"/> entry to the history.</summary>
    /// <param name="state">State whose name should be added.</param>
    public void OnStateEntered(State state)
    {
        if (!IgnoreStateChanges)
            _history.AddStateEntered(Engine.GetProcessFrames(), state.Name);
    }

    /// <summary>Add a <see cref="State"/> exit to the history.</summary>
    /// <param name="state">State whose name should be added.</param>
    public void OnStateExited(State state)
    {
        if (!IgnoreStateChanges)
            _history.AddStateExited(Engine.GetProcessFrames(), state.Name);
    }

    /// <summary>Add a taken <see cref="Transition"/> to the history.</summary>
    /// <param name="transition">Transition to add.</param>
    /// <param name="from">State the transition was from.</param>
    public void OnBeforeTransition(Transition transition, State from)
    {
        if (!IgnoreTransitions)
            _history.AddTransition(Engine.GetProcessFrames(), transition.Name, StateChart.GetPathTo(from), StateChart.GetPathTo(transition.To));
    }

    /// <summary>Update the UI if it's been changed.</summary>
    public void OnTimerTimeout()
    {
        if (HistoryEdit.Visible && _history.Dirty)
        {
            HistoryEdit.Text = _history.GetHistoryText();
            HistoryEdit.ScrollVertical = HistoryEdit.GetLineCount() - 1;
        }
    }

    public void OnIgnoreEventsCheckBoxToggled(bool pressed) => IgnoreEvents = pressed;
    public void OnIgnoreStateChangesCheckBoxToggled(bool pressed) => IgnoreStateChanges = pressed;
    public void OnIgnoreTransitionsCheckBoxToggled(bool pressed) => IgnoreTransitions = pressed;

    public override void _Ready()
    {
        base._Ready();

        ProcessMode = ProcessModeEnum.Always;

        _history = new History { MaximumLines = MaximumLines };

        GetNode<Button>("%CopyToClipboardButton").Pressed += () => DisplayServer.ClipboardSet(HistoryEdit.Text);
        GetNode<Button>("%ClearButton").Pressed += ClearHistory;

        InspectChart(StateChart);

        GetNode<CheckBox>("%IgnoreEventsCheckBox").SetPressedNoSignal(IgnoreEvents);
        GetNode<CheckBox>("%IgnoreStateChangesCheckBox").SetPressedNoSignal(IgnoreStateChanges);
        GetNode<CheckBox>("%IgnoreTransitionsCheckBox").SetPressedNoSignal(IgnoreTransitions);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Tree.Clear();

        if (!IsInstanceValid(StateChart))
            return;
        
        TreeItem root = Tree.CreateItem();
        root.SetText(0, StateChart.Name);

        CollectActiveStates(StateChart, root);

        if (!StateChart.ExpressionProperties.IsEmpty)
        {
            ImmutableList<StringName> properties = StateChart.ExpressionProperties.Keys.ToImmutableList().Sort(static (a, b) => string.Compare(a, b));

            TreeItem propertiesRoot = root.CreateChild();
            propertiesRoot.SetText(0, "< Expression properties >");
            foreach (StringName property in properties)
            {
                TreeItem line = propertiesRoot.CreateChild();
                line.SetText(0, $"{property} = {StateChart.ExpressionProperties[property]}");
            }
        }
    }
}