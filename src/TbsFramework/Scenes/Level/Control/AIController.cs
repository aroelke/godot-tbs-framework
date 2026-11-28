using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Control.Evaluation;
using TbsFramework.Scenes.Rendering;
using TbsFramework.Scenes.Transitions;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Automatically controls units based on their <see cref="BehaviorNode"/>s and the state of the level.</summary>
[Icon("uid://bambqlpd7p4kr"), Tool]
public partial class AIController : ArmyController
{
    /// <summary>Signals that the fast-forward state has changed.</summary>
    /// <param name="enable"><c>true</c> if fast-forwarding is in progress, and <c>false</c> otherwise.</param>
    [Signal] public delegate void FastForwardStateChangedEventHandler(bool enable);

    private readonly NodeCache _cache = null;
    private UnitData _selected = null;
    private Vector2I _destination = GridData.InvalidCell;
    private UnitAction _action = null;
    private UnitData _target = null;
    private bool _ff = false;

    private Sprite2D              Pseudocursor          => _cache.GetNode<Sprite2D>("Pseudocursor");
    private FadeToBlackTransition FastForwardTransition => _cache.GetNode<FadeToBlackTransition>("CanvasLayer/FastForwardTransition");
    private Timer                 IndicatorTimer        => _cache.GetNode<Timer>("IndicatorTimer");

    public override Grid Grid { get; set; } = null;

    [Export] public ActionSelector Selector = null;

    /// <summary>Sprite to use for the pseudocursor.</summary>
    [Export] public Texture2D CursorSprite
    {
        get => Pseudocursor?.Texture;
        set
        {
            if (Pseudocursor is not null)
                Pseudocursor.Texture = value;
        }
    }

    /// <summary>Pseudocursor sprite offset from the origin of the texture to use for positioning it within a cell.</summary>
    [Export] public Vector2 CursorOffset
    {
        get => Pseudocursor?.Offset ?? Vector2.Zero;
        set
        {
            if (Pseudocursor is not null)
                Pseudocursor.Offset = value;
        }
    }

    /// <summary>Time in seconds to hold the cursor over an indicated cell before acting on it.</summary>
    [Export(PropertyHint.None, "suffix:s")] public float IndicationTime = 0.5f;

    /// <summary>Whether or not this army's turn can be skipped.</summary>
    [Export] public bool EnableTurnSkipping = true;

    /// <summary>Maximum number of levels in the action tree to search for the best action.</summary>
    /// <remarks><b>Warning</b>: Be careful of increasing this higher than 3, or even 2 in some cases, as it can significantly hurt performance.</remarks>
    [Export(PropertyHint.Range, "1,3,or_greater,or_less")] public int MaxSearchDepth = 3;

    /// <summary>Performance option to evaluate moves using threads.</summary>
    /// <remarks>Note: The exact number of threads is based on the C# <see cref="Task"/> pool.</remarks>
    [Export] public bool EvaluateWithThreads = true;

    public AIController() : base() { _cache = new(this); }

    public override void InitializeTurn()
    {
        _selected = null;
        _destination = GridData.InvalidCell;
        _action = null;
        _target = null;

        EmitSignal(SignalName.ProgressUpdated, 0, Faction.GetUnits(Grid.Data).Count());
        EmitSignal(SignalName.EnabledInputActionsUpdated, new StringName[] {InputManager.Skip});
    }

    /// <inheritdoc/>
    /// <remarks>Unit actions will still be calculated and the results updated. The screen will be blacked out while computing actions.</remarks>
    public override void FastForwardTurn()
    {
        EmitSignal(SignalName.EnabledInputActionsUpdated, Array.Empty<StringName>());
        FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.FastForwardStateChanged, _ff = true), (uint)ConnectFlags.OneShot);
        FastForwardTransition.TransitionOut();
    }

    public override async void SelectUnit(UnitAction[] actions)
    {
        SpecifiedAction? action = await Task.Run(() => Selector.SelectAction(Grid.Data, Faction, actions.Where((a) => a.AIAllowed)));
        if (action is not null)
        {
            (_selected, _action, _destination, Vector2I target, _) = action.Value;
            if (Grid.Data.Occupants.TryGetValue(target, out UnitData unit))
                _target = unit;
            else
                _target = null;
        }
        else
        {
            _selected = Selector.GetDefaultUnit(Grid.Data, Faction);
            _action = null;
            _destination = GridData.InvalidCell;
            _target = null;
        }

        EmitSignal(SignalName.UnitSelected, _selected.Cell);
    }

    public override void MoveUnit(UnitData unit, UnitAction[] actions)
    {
        if (_destination == GridData.InvalidCell)
            _destination = Selector.GetDefaultDestination(_selected);
        void ConfirmMove() => EmitSignal(SignalName.PathConfirmed, unit.Cell, new Godot.Collections.Array<Vector2I>(unit.Behavior.GetPath(unit, _destination)));
        if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, ConfirmMove, (uint)ConnectFlags.OneShot);
        else
            ConfirmMove();
    }

    public override void CommandUnit(UnitData source, UnitAction[] commands, UnitAction cancel)
    {
        _action ??= commands.FirstOrDefault((a) => a.AIAllowed && !a.RequiresTarget && a.CanPerform(_selected, _destination));
        EmitSignal(SignalName.UnitCommanded, source.Cell, _action);
    }

    public override void SelectTarget(UnitData source, IEnumerable<Vector2I> targets)
    {
        if (_target is null)
            throw new InvalidOperationException($"{source.Renderer.Name}'s target has not been determined");
        if (!targets.Contains(_target.Cell))
            throw new InvalidOperationException($"{source.Renderer.Name} can't target {_target.Renderer}");

        Pseudocursor.Position = Grid.PositionOf(_target.Cell);
        Pseudocursor.Visible = true;

        if (_ff)
            EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell);
        else if (FastForwardTransition.Active)
            FastForwardTransition.Connect(SceneTransition.SignalName.TransitionedOut, () => EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell), (uint)ConnectFlags.OneShot);
        else
        {
            IndicatorTimer.Connect(Timer.SignalName.Timeout, () => EmitSignal(SignalName.TargetChosen, source.Cell, _target.Cell), (uint)ConnectFlags.OneShot);
            IndicatorTimer.WaitTime = IndicationTime;
            IndicatorTimer.Start();
        }
    }

    public override void FinalizeAction()
    {
        Pseudocursor.Visible = false;
        IEnumerable<UnitData> units = Faction.GetUnits(Grid.Data);
        EmitSignal(
            SignalName.ProgressUpdated,
            units.Count(static (u) => !u.Active) + 1, // Add one to account for the unit that just finished
            units.Count(static (u) => u.Active) - 1
        );
    }

    public override void FinalizeTurn()
    {
        base.FinalizeTurn();
        FastForwardTransition.TransitionIn();
        EmitSignal(SignalName.FastForwardStateChanged, _ff = false);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (!Engine.IsEditorHint() && EnableTurnSkipping && @event.IsActionPressed(InputManager.Cancel))
            EmitSignal(SignalName.TurnFastForward);
    }
}