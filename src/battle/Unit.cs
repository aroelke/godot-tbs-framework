using System.Linq;
using Godot;

namespace battle;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and interface for the player to
/// interact.
/// </summary>
public partial class Unit : Path2D
{
    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private BattleMap _map = null;
    private Vector2I _cell = Vector2I.Zero;
    private AnimatedSprite2D _sprite = null;
    private bool _selected = false;
    private PathFollow2D _follow = null;
    private bool _moving = false;

    private BattleMap Map => _map ??= GetParent<BattleMap>();
    private AnimatedSprite2D Sprite => _sprite ??= GetNode<AnimatedSprite2D>("PathFollow/Sprite");
    private PathFollow2D PathFollow => _follow ??= GetNode<PathFollow2D>("PathFollow");

    /// <summary>Movement range of the unit, in grid cells.</summary>
    [Export] public int MoveRange = 5;

    /// <summary>Speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

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

    /// <summary>Whether or not the unit is currently moving along the path.</summary>
    public bool IsMoving
    {
        get => _moving;
        set => SetProcess(_moving = value);
    }

    /// <summary>Move the unit along a path of map cells.  Cells should be contiguous.</summary>
    /// <param name="path">Coordinates of the cells to move along.</param>
    public void MoveAlong(Vector2I[] path)
    {
        if (path.Length > 0)
        {
            foreach (Vector2I cell in path)
                Curve.AddPoint(Map.PositionOf(cell) - Position);
            Cell = path.Last();
            IsMoving = true;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Cell = Map.CellOf(Position);
        Position = Map.PositionOf(Cell);

        if (!Engine.IsEditorHint())
            Curve = new();

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 prev = PathFollow.Position;
        PathFollow.Progress += (float)(MoveSpeed*delta);
        (string animation, bool flip) = (PathFollow.Position - prev) switch
        {
            Vector2(_, <0) => ("up", false),
            Vector2(<0, _) => ("side", false),
            Vector2(_, >0) => ("down", false),
            Vector2(>0, _) => ("side", true),
            _ => ("idle", false)
        };
        if (Sprite.Animation != animation)
        {
            Sprite.Play(animation);
            Sprite.FlipH = flip;
        }

        if (PathFollow.ProgressRatio >= 1)
        {
            IsMoving = false;
            PathFollow.Progress = 0;
            Position = Map.PositionOf(Cell);
            Curve.ClearPoints();
            EmitSignal(SignalName.DoneMoving);
        }
    }
}