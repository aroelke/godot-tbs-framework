using Godot;

namespace battle;

public partial class BattleMouse : ui.VirtualMouse
{
    private BattleMap _map = null;
    private BattleMap Map => _map ??= GetParent<BattleMap>();

    /// <summary>Clamp a position to somewhere with in the map, not necessarily snapped to a grid cell.</summary>
    /// <param name="position">Position to clamp.</param>
    /// <returns>A new position computed by clamping the old one using <c>Map.Clamp</c>.</returns>
    public override Vector2 Clamp(Vector2 position) => Map.Clamp(position);

    /// <summary>Jump one grid cell in a direction.</summary>
    /// <param name="direction">Direction to jump. Each component should have a magnitude of 0 or 1.</param>
    public override void Jump(Vector2I direction)
    {
        Position = Map.PositionOf(Map.Clamp(Map.CellOf(Position) + direction)) + Map.CellSize/2;
    }

    /// <summary>Skip to the map edge in a direction.</summary>
    /// <param name="direction">Direction to skip. Each component should have a magnitude of 0 or 1.</param>
    public override void Skip(Vector2I direction)
    {
        Position = Map.PositionOf(new(
            direction.X < 0 ? 0 : direction.X > 0 ? Map.Size.X - 1 : 0,
            direction.Y < 0 ? 0 : direction.Y > 0 ? Map.Size.Y - 1 : 0
        )) + Map.CellSize/2;
    }
}