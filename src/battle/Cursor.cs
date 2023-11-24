using Godot;

namespace battle;

/// <summary>The cursor on the map that allows the player to interact with it.</summary>
public partial class Cursor : Sprite2D
{
    private BattleMap _map = null;
    private Vector2I _cell = Vector2I.Zero;
    private Vector2I _mask = Vector2I.Zero;

    private BattleMap Map { get => GetParent<BattleMap>(); }

    /// <summary>Grid cell containing the cursor.</summary>
    public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I clamped = Map.Clamp(value);
            if (clamped != _cell)
            {
                _cell = clamped;
                Position = Map.PositionOf(_cell);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseMotion mm)
            Cell = Map.CellOf(mm.Position);
        else
        {
            Vector2I target = (Vector2I)Input.GetVector("cursor_left", "cursor_right", "cursor_up", "cursor_down").Round();
            if (!@event.IsEcho() && target != _mask)
                target = new(_mask.X == 0 ? target.X : 0, _mask.Y == 0 ? target.Y : 0);
            if (target != Vector2.Zero)
                Cell += target;
            if (!@event.IsEcho())
            {
                if (@event.IsPressed())
                    _mask = new(_mask.X | target.Abs().X, _mask.Y | target.Abs().Y);
                else if (@event.IsReleased())
                    _mask = new(_mask.X & target.Abs().X, _mask.Y & target.Abs().Y);
                else
                    _mask = Vector2I.Zero;
            }
        }
    }
}