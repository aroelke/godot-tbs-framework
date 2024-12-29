using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Level.Control;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.UI;
using TbsTemplate.UI.Controls.Device;

[SceneTree, Tool]
public partial class PlayerController : ArmyController
{
    private static readonly StringName SelectEvent = "Select";
    private static readonly StringName FinishEvent = "Finish";

    private readonly DynamicEnumProperties<StringName> _events = new([SelectEvent, FinishEvent]);
    private Unit _selected = null, _target = null;
    IEnumerable<Vector2I> _traversable = null, _attackable = null, _supportable = null;
    private Path _path;
    private ContextMenu _menu;

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
        ContextMenu menu = ContextMenu.Instantiate(options);
        menu.Wrap = true;
        GetNode<CanvasLayer>("UserInterface").AddChild(menu);
        menu.Visible = false;
        menu.MenuClosed += () => {
            Cursor.Resume();
//            Camera.Target = _prevCameraTarget;
//            _prevCameraTarget = null;
        };

        Cursor.Halt(hide:true);
//        _prevCameraTarget = Camera.Target;
//        Camera.Target = null;

        Callable.From<ContextMenu, Rect2>((m, r) => {
            m.Visible = true;
            if (DeviceManager.Mode != InputMode.Mouse)
                m.GrabFocus();
            m.Position = MenuPosition(r, m.Size);
        }).CallDeferred(menu, rect);

        return menu;
    }

    private void ConfirmCursorSelection(Vector2I cell)
    {
        if (Cursor.Grid.Occupants.TryGetValue(cell, out GridNode node) && node is Unit unit && unit.Faction == Army.Faction)
        {
            EmitSignal(SignalName.UnitSelected, unit);
            Cursor.CellSelected -= ConfirmCursorSelection;
        }
    }

    private void AddToPath(Vector2I cell)
    {
        void UpdatePath(Path path) => EmitSignal(SignalName.PathUpdated, new Godot.Collections.Array<Vector2I>(_path = path));

        // If the previous cell was an ally that could be supported and moved through, add it to the path as if it
        // had been added in the previous movement
        if (_target is not null && _supportable.Contains(_target.Cell) && _traversable.Contains(_target.Cell))
            UpdatePath(_path.Add(_target.Cell));

        _target = null;
//        _command = null;

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
/*
                if (_attackable.Contains(cell))
                    _command = AttackLayer;
                else if (_supportable.Contains(cell))
                    _command = SupportLayer;
*/

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
        if (!Cursor.Grid.Occupants.ContainsKey(cell) || Cursor.Grid.Occupants[cell] == _selected)
        {
            EmitSignal(SignalName.UnitMoved, new Godot.Collections.Array<Vector2I>(_path));
        }
    }

    public override void InitializeTurn()
    {
        Cursor.Resume();
    }

    public override void SelectUnit() => State.SendEvent(_events[SelectEvent]);

    public void OnSelectEntered()
    {
        Cursor.CellSelected += ConfirmCursorSelection;
    }

    public override void MoveUnit(Unit unit)
    {
        State.SendEvent(_events[FinishEvent]);

        _target = null;
        _selected = unit;
        (_traversable, _attackable, _supportable) = unit.ActionRanges();
        _path = Path.Empty(Cursor.Grid, _traversable).Add(_selected.Cell);
        Cursor.CellChanged += AddToPath;
        Cursor.CellSelected += ConfirmPathSelection;
    }

    public override void EndMove()
    {
        Cursor.CellChanged -= AddToPath;
        Cursor.CellSelected -= ConfirmPathSelection;
    }

    public override void CommandUnit(Unit source, Godot.Collections.Array<StringName> commands, StringName cancel)
    {
        _selected = source;
        _menu = ShowMenu(Cursor.Grid.CellRect(source.Cell), commands.Select((c) => new ContextMenuOption() { Name = c, Action = () => EmitSignal(SignalName.UnitCommanded, c) }));
        _menu.MenuCanceled += () => EmitSignal(SignalName.UnitCommanded, cancel);
        _menu.MenuClosed += () => _menu = null;
    }

    public override void FinalizeTurn() => _selected = null;

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
