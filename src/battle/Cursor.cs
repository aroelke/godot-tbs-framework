using Godot;

namespace battle;

/// <summary>The cursor on the map that allows the player to interact with it.</summary>
public partial class Cursor : Sprite2D
{
    private BattleMap _map = null;
    private Vector2I _cell = Vector2I.Zero;

    private BattleMap Map => _map ??= GetParent<BattleMap>();

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

    /// <summary>Update the cursor position to the cell containing the mouse.</summary>
    /// <param name="previous">Previous mouse position (unused)</param>
    /// <param name="current">Current mouse position where the cursor will be moved to.</param>
    public void OnMouseMoved(Vector2 previous, Vector2 current)
    {
        Cell = Map.CellOf(current);
    }
}