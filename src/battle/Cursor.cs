using Godot;

namespace battle;

/// <summary>The cursor on the map that allows the player to interact with it.</summary>
public partial class Cursor : Sprite2D
{
    private BattleMap _map = null;
    private Vector2I _cell = Vector2I.Zero;

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
    }
}