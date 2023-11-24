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
            Vector2I skip = (Vector2I)Input.GetVector("cursor_skip_left", "cursor_skip_right", "cursor_skip_up", "cursor_skip_down").Round();
            if (skip != Vector2.Zero)
                Cell = new(
                    skip.X < 0 ? 0 : skip.X > 0 ? Map.Size.X - 1 : Cell.X,
                    skip.Y < 0 ? 0 : skip.Y > 0 ? Map.Size.Y - 1 : Cell.Y
                );

            Vector2I move = (Vector2I)Input.GetVector("cursor_left", "cursor_right", "cursor_up", "cursor_down").Round();
            if (!@event.IsEcho() && move != _mask)
                move = new(_mask.X == 0 ? move.X : 0, _mask.Y == 0 ? move.Y : 0);
            if (move != Vector2.Zero)
                Cell += move;
            if (!@event.IsEcho())
            {
                if (@event.IsPressed())
                    _mask = new(_mask.X | move.Abs().X, _mask.Y | move.Abs().Y);
                else if (@event.IsReleased())
                    _mask = new(_mask.X & move.Abs().X, _mask.Y & move.Abs().Y);
                else
                    _mask = Vector2I.Zero;
            }
        }
    }
}