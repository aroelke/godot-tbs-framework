using Godot;

namespace battle;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and interface for the player to
/// interact.
/// </summary>
public partial class Unit : Path2D
{
    private BattleMap _map = null;
    private Vector2I _cell = Vector2I.Zero;
    private AnimatedSprite2D _sprite = null;
    private bool _selected = false;

    private BattleMap Map => _map ??= GetParent<BattleMap>();
    private AnimatedSprite2D Sprite => _sprite ??= GetNode<AnimatedSprite2D>("PathFollow/Sprite");

    /// <summary>Cell on the grid that this unit currently occupies.</summary>
    public Vector2I Cell
    {
        get => _cell;
        set => _cell = Map.Clamp(value);
    }

    /// <summary>Whether or not this unit has been selected and is awaiting instruction. Changing it toggles the selected animation.</summary>
    public bool IsSelected
    {
        get => _selected;
        set
        {
            _selected = value;
            if (_selected)
                Sprite.Play("selected");
            else
                Sprite.Play("idle");
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Cell = Map.CellOf(Position);
        Position = Map.PositionOf(Cell);

        SetProcess(false);
    }
}