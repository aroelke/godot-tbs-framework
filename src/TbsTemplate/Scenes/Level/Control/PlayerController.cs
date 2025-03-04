using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI;
using TbsTemplate.UI.Controls.Action;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.Scenes.Level.Control;

[SceneTree, Tool]
public partial class PlayerController : ArmyController
{
    private static readonly StringName SelectEvent  = "Select";
    private static readonly StringName PathEvent    = "Path";
    private static readonly StringName CommandEvent = "Command";
    private static readonly StringName TargetEvent  = "Target";
    private static readonly StringName FinishEvent  = "Finish";
    private static readonly StringName CancelEvent  = "Cancel";

    private readonly DynamicEnumProperties<StringName> _events = new([SelectEvent, PathEvent, CommandEvent, TargetEvent, FinishEvent, CancelEvent]);
    private Grid _grid = null;
    private Unit _selected = null, _target = null;
    IEnumerable<Vector2I> _traversable = null, _attackable = null, _supportable = null;
    private Path _path;
    private ContextMenu _menu;

    public override Grid Grid
    {
        get => _grid;
        set
        {
            if (_grid != value)
            {
                _grid = value;
                if (Cursor is not null)
                    Cursor.Grid = _grid;
                if (Pointer is not null)
                    Pointer.World = _grid;
            }
        }
    }
#region Menus
    private Vector2 MenuPosition(Rect2 rect, Vector2 size)
    {
        Rect2 viewportRect = Cursor.Grid.GetGlobalTransformWithCanvas()*rect;
        float viewportCenter = GetViewport().GetVisibleRect().Position.X + GetViewport().GetVisibleRect().Size.X/2;
        return new(
            viewportCenter - viewportRect.Position.X < viewportRect.Size.X/2 ? viewportRect.Position.X - size.X : viewportRect.End.X,
            Mathf.Clamp(viewportRect.Position.Y - (size.Y - viewportRect.Size.Y)/2, 0, GetViewport().GetVisibleRect().Size.Y - size.Y)
        );
    }

    private ContextMenu ShowMenu(Rect2 rect, IEnumerable<ContextMenuOption> options)
    {
        Cursor.Halt(hide:true);
        Pointer.StartWaiting(hide:false);
        CancelHint.Visible = true;

        ContextMenu menu = ContextMenu.Instantiate(options);
        menu.Wrap = true;
        UserInterface.AddChild(menu);
        menu.Visible = false;
        menu.MenuClosed += () => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.RevertCameraFocus);

        LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, (BoundedNode2D)null);

        Callable.From<ContextMenu, Rect2>((m, r) => {
            m.Visible = true;
            if (DeviceManager.Mode != InputMode.Mouse)
                m.GrabFocus();
            m.Position = MenuPosition(r, m.Size);
        }).CallDeferred(menu, rect);

        return menu;
    }
#endregion
#region Initialization and Finalization
    public override void InitializeTurn()
    {
        HUD.Visible = true;

        Cursor.Cell = ((IEnumerable<Unit>)Army).First().Cell;

        Cursor.Resume();
        Pointer.StopWaiting();
    }

    public override void FinalizeAction() {}

    public override void FinalizeTurn()
    {
        Cursor.Halt(hide:true);
        Pointer.StartWaiting(hide:true);
        HUD.Visible = false;
    }
#endregion
#region State Independent
    public void OnCancel() => CancelSound.Play();
    public void OnFinish() => SelectSound.Play();

    public void OnCursorCellChanged(Vector2I cell) => EmitSignal(SignalName.CursorCellChanged, cell);
    public void OnCursorCellEntered(Vector2I cell) => EmitSignal(SignalName.CursorCellEntered, cell);

    public void OnIdlePointerStopped(Vector2 position) => EmitSignal(SignalName.CursorCellEntered, Grid.CellOf(position));
    public void OnPointerFlightStarted(Vector2 target) => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, target);
    public void OnPointerFlightCompleted() => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.RevertCameraFocus);
#endregion
#region Active
    public void OnActiveInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.Cancel))
        {
            State.SendEvent(_events[CancelEvent]);
            EmitSignal(SignalName.SelectionCanceled);
        }

        if (@event.IsActionPressed(InputActions.ToggleDangerZone))
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ToggleDangerZone, Army, Cursor.Grid.Occupants.GetValueOrDefault(Cursor.Cell) as Unit);
    }

    public void OnActiveExited() => Callable.From(() => _selected = null).CallDeferred();
#endregion
#region Unit Selection
    public override void SelectUnit() => Callable.From(() => State.SendEvent(_events[SelectEvent])).CallDeferred();

    private void ConfirmCursorSelection(Vector2I cell)
    {
        if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit unit)
        {
            if (unit.Army.Faction == Army.Faction && unit.Active)
            {
                State.SendEvent(_events[FinishEvent]);
                EmitSignal(SignalName.UnitSelected, unit);
            }
            else if (unit.Army.Faction != Army.Faction)
                LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.ToggleDangerZone, Army, unit);
        }
        else
        {
            void Cancel()
            {
                CancelSound.Play();
                Cursor.Resume();
                Pointer.StopWaiting();
            }

            SelectSound.Play();
            ContextMenu menu = ShowMenu(Cursor.Grid.CellRect(cell), [
                new("End Turn", () => {
                    // Cursor is already halted
                    Pointer.StartWaiting(hide:true);

                    State.SendEvent(_events[FinishEvent]);
                    EmitSignal(SignalName.TurnSkipped);
                    SelectSound.Play();
                }),
                new("Quit Game", () => GetTree().Quit()),
                new("Cancel", Cancel)
            ]);
            menu.MenuCanceled += Cancel;
            menu.MenuClosed += () => CancelHint.Visible = false;
        }
    }

    public void OnSelectEntered()
    {
        Cursor.Resume();
        Pointer.StopWaiting();
        CancelHint.Visible = false;

        Cursor.Cell = Grid.CellOf(Pointer.Position);
        EmitSignal(SignalName.CursorCellEntered, Cursor.Cell);
        Cursor.CellSelected += ConfirmCursorSelection;
    }

    public void OnSelectInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.Previous) || @event.IsActionPressed(InputActions.Next))
        {
            if (Cursor.Grid.Occupants.GetValueOrDefault(Cursor.Cell) is Unit hovered)
            {
                if (@event.IsActionPressed(InputActions.Previous) && hovered.Army.Previous(hovered) is Unit prev)
                    Cursor.Cell = prev.Cell;
                if (@event.IsActionPressed(InputActions.Next) && hovered.Army.Next(hovered) is Unit next)
                    Cursor.Cell = next.Cell;
            }
            else
            {
                IEnumerable<Unit> units = Army.GetChildren().OfType<Unit>().Where((u) => u.Active);
                if (!units.Any())
                    units = Army.GetChildren().OfType<Unit>();
                if (units.Any())
                    Cursor.Cell = units.OrderBy((u) => u.Cell.DistanceTo(Cursor.Cell)).First().Cell;
            }
        }

        if (@event.IsActionPressed(InputActions.Cancel) && Cursor.Grid.Occupants.TryGetValue(Cursor.Cell, out GridNode node) && node is Unit untrack)
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.RemoveFromDangerZone, Army, untrack);
    }

    public void OnSelectExited() => Cursor.CellSelected -= ConfirmCursorSelection;
#endregion
#region Path Selection
    private StringName _command = null;

    public override void MoveUnit(Unit unit)
    {
        Callable.From(() => {
            _selected = unit;
            State.SendEvent(_events[PathEvent]);
        }).CallDeferred();
    }

    private void UpdatePath(Path path) => EmitSignal(SignalName.PathUpdated, _selected, new Godot.Collections.Array<Vector2I>(_path = path));

    private void AddToPath(Vector2I cell)
    {
        // If the previous cell was an ally that could be supported and moved through, add it to the path as if it
        // had been added in the previous movement
        if (_target is not null && _supportable.Contains(_target.Cell) && _traversable.Contains(_target.Cell))
            UpdatePath(_path.Add(_target.Cell));

        _target = null;
        _command = null;

        IEnumerable<Vector2I> sources = [];
        if (Cursor.Grid.Occupants.GetValueOrDefault(cell) is Unit target)
        {
            // Compute cells the highlighted unit could be targeted from (friend or foe)
            if (target != _selected && Army.Faction.AlliedTo(target) && _supportable.Contains(cell))
                sources = _selected.SupportableCells(cell).Where(_traversable.Contains);
            else if (!Army.Faction.AlliedTo(target) && _attackable.Contains(cell))
                sources = _selected.AttackableCells(cell).Where(_traversable.Contains);
            sources = sources.Where((c) => !Cursor.Grid.Occupants.ContainsKey(c) || Cursor.Grid.Occupants[c] == _selected);

            if (sources.Any())
            {
                _target = target;

                // Store the action command related to selecting the target as if it were the command state
                if (_attackable.Contains(cell))
                    _command = "Attack";
                else if (_supportable.Contains(cell))
                    _command = "Support";

                // If the end of the path isn't a cell that could act on the target, find the furthest one that can and add
                // it to the path
                if (!sources.Contains(_path[^1]))
                {
                    UpdatePath(sources.Select((c) => _path.Add(c).Clamp(_selected.Stats.Move)).OrderBy(
                        (p) => new Vector2I(-(int)p[^1].DistanceTo(cell), (int)p[^1].DistanceTo(_path[^1])),
                        static (a, b) => a < b ? -1 : a > b ? 1 : 0
                    ).First());
                }
            }
        }
        if (!sources.Any() && _traversable.Contains(cell))
            UpdatePath(_path.Add(cell).Clamp(_selected.Stats.Move));
    }

    private void ConfirmPathSelection(Vector2I cell)
    {
        bool occupied = Cursor.Grid.Occupants.TryGetValue(cell, out GridNode occupant);
        if (!_traversable.Contains(cell) && !occupied)
        {
            State.SendEvent(_events[CancelEvent]);
            EmitSignal(SignalName.SelectionCanceled);
        }
        else if (!occupied || occupant == _selected)
        {
            Cursor.Halt(hide:false);
            Pointer.StartWaiting(hide:false);
            CancelHint.Visible = false;

            State.SendEvent(_events[FinishEvent]);
            EmitSignal(SignalName.PathConfirmed, _selected, new Godot.Collections.Array<Vector2I>(_path));
        }
        else if (occupied && occupant is Unit target && (_attackable.Contains(target.Cell) || _supportable.Contains(target.Cell)))
        {
            Cursor.Halt(hide:false);
            Pointer.StartWaiting(hide:false);
            CancelHint.Visible = false;

            State.SendEvent(_events[FinishEvent]);
            EmitSignal(SignalName.UnitCommanded, _selected, _command);
            EmitSignal(SignalName.TargetChosen, _selected, target);
            EmitSignal(SignalName.PathConfirmed, _selected, new Godot.Collections.Array<Vector2I>(_path));
        }
        else
            ErrorSound.Play();
    }

    public void OnPathEntered()
    {
        Cursor.Resume();
        Pointer.StopWaiting();
        CancelHint.Visible = true;

        _target = null;
        (_traversable, _attackable, _supportable) = _selected.ActionRanges();
        UpdatePath(Path.Empty(Cursor.Grid, _traversable).Add(_selected.Cell));
        Cursor.CellChanged += AddToPath;
        Cursor.CellSelected += ConfirmPathSelection;
        Cursor.SoftRestriction = [.. _traversable];
        Cursor.Cell = _selected.Cell;
    }

    public void OnPathExited()
    {
        Cursor.SoftRestriction.Clear();
        Cursor.CellChanged -= AddToPath;
        Cursor.CellSelected -= ConfirmPathSelection;
    }
#endregion
#region Command Selection
    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        Callable.From(() => {
            _selected = source;
            _menu = ShowMenu(Cursor.Grid.CellRect(source.Cell), commands.Select((c) => new ContextMenuOption() { Name = c, Action = () => {
                State.SendEvent(_events[FinishEvent]);
                EmitSignal(SignalName.UnitCommanded, source, c);
            }}));
            _menu.MenuCanceled += () => EmitSignal(SignalName.UnitCommanded, source, cancel);
            _menu.MenuClosed += () => _menu = null;
            State.SendEvent(_events[CommandEvent]);
        }).CallDeferred();
    }

    public void OnCommandEntered() => CancelHint.Visible = true;

    public void OnCommandProcess(double delta) => _menu.Position = MenuPosition(Cursor.Grid.CellRect(_selected.Cell), _menu.Size);
#endregion
#region Target Selection
    private IEnumerable<Vector2I> _targets = null;

    public override void SelectTarget(Unit source, IEnumerable<Vector2I> targets)
    {
        Callable.From(() => {
            _selected = source;
            _targets = targets;
            State.SendEvent(_events[TargetEvent]);
        }).CallDeferred();
    }

    private void ConfirmTargetSelection(Vector2I cell)
    {
        if (Cursor.Cell != Grid.CellOf(Pointer.Position))
        {
            State.SendEvent(_events[CancelEvent]);
            EmitSignal(SignalName.TargetCanceled, _selected);
        }
        else if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit target)
        {
            Cursor.Halt(hide:false);
            Pointer.StartWaiting(hide:false);
            CancelHint.Visible = false;

            State.SendEvent(_events[FinishEvent]);
            EmitSignal(SignalName.TargetChosen, _selected, target);
        }
    }

    public void OnTargetEntered()
    {
        Cursor.Resume();
        Pointer.StopWaiting();
        CancelHint.Visible = true;
        Cursor.CellSelected += ConfirmTargetSelection;

        Pointer.AnalogTracking = false;
        Cursor.HardRestriction = [.. _targets];
        Cursor.Wrap = true;
    }

    public void OnTargetInput(InputEvent @event)
    {
        int next = 0;
        if (@event.IsActionPressed(InputActions.Previous))
            next = -1;
        else if (@event.IsActionPressed(InputActions.Next))
            next = 1;

        if (next != 0)
        {
            if (_targets.Contains(Cursor.Cell))
            {
                if (_targets.Count() > 1)
                {
                    Vector2I[] cells = [.. _targets];
                    Cursor.Cell = cells[(Array.IndexOf(cells, Cursor.Cell) + next + cells.Length) % cells.Length];
                }
            }
            else
                GD.PushError("Cursor is not on an actionable cell during targeting");
        }
    }

    public void OnTargetExited()
    {
        Cursor.CellSelected -= ConfirmTargetSelection;

        Pointer.AnalogTracking = true;
        Cursor.HardRestriction = Cursor.HardRestriction.Clear();
        Cursor.Wrap = false;
    }
#endregion
#region Engine Events
    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
        {
            LevelEvents.Singleton.Connect<Rect2I>(LevelEvents.SignalName.CameraBoundsUpdated, (b) => Pointer.Bounds = b);
            Callable.From(() => LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.FocusCamera, Pointer)).CallDeferred();
            Cursor.Halt();
        }
    }
#endregion
#region Properties
    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = base._GetPropertyList() ?? [];
        properties.AddRange(_events.GetPropertyList(State.Events));
        return properties;
    }

    public override Variant _Get(StringName property)
    {
        if (_events.TryGetPropertyValue(property, out StringName value))
            return value;
        else
            return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (value.VariantType == Variant.Type.StringName && _events.SetPropertyValue(property, value.AsStringName()))
            return true;
        else
            return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        if (_events.PropertyCanRevert(property, out bool revert))
            return revert;
        else
            return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (_events.TryPropertyGetRevert(property, out StringName revert))
            return revert;
        else
            return base._PropertyGetRevert(property);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_menu is not null)
            _menu.Position = MenuPosition(Cursor.Grid.CellRect(_selected.Cell), _menu.Size);
    }
}
#endregion