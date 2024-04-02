using System.Collections.Generic;
using System.Linq;
using Godot;
using Level.Object.Group;
using Object;

namespace Level.Object;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and interface for the player to
/// interact.
/// </summary>
[Tool]
public partial class Unit : GridNode
{
    // AnimationTree parameters
    private static readonly StringName Idle = "parameters/conditions/idle";
    private static readonly StringName Selected = "parameters/conditions/selected";
    private static readonly StringName Moving = "parameters/conditions/moving";
    private static readonly StringName MoveDirection = "parameters/Moving/blend_position";

    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private Path2D _path = null;
    private PathFollow2D _follow = null;
    private Sprite2D _sprite = null;
    private Army _affiliation = null;
    private AnimationTree _tree = null;

    private Path2D Path => _path = GetNode<Path2D>("Path");
    private PathFollow2D PathFollow => _follow = GetNode<PathFollow2D>("Path/PathFollow");

    /// <summary>Movement range of the unit, in grid cells.</summary>
    [Export] public int MoveRange = 5;

    /// <summary>Distances from the unit's occupied cell that it can attack.</summary>
    [Export] public int[] AttackRange = new[] { 1, 2 };

    /// <summary>Distances from the unit's occupied cell that it can support.</summary>
    [Export] public int[] SupportRange = new[] { 1, 2, 3 };

    /// <summary>Army to which this unit belongs, to determine its allies and enemies.</summary>
    [Export] public Army Affiliation
    {
        get => _affiliation;
        set
        {
            _affiliation = value;
            if (_sprite is not null && _affiliation is not null)
                _sprite.Modulate = _affiliation.Color;
        }
    }

    /// <summary>Speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

    /// <summary>Put the unit in the "selected" state.</summary>
    public void Select()
    {
        _tree.Set(Idle, false);
        _tree.Set(Selected, true);
        _tree.Set(Moving, false);
    }

    /// <summary>Put the unit in the "idle" state.</summary>
    public void Deselect()
    {
        _tree.Set(Idle, true);
        _tree.Set(Selected, false);
        _tree.Set(Moving, false);
    }

    /// <summary>
    /// Box that travels with the motion of the sprite to use for tracking the unit as it moves.  Don't use the unit's actual position, as that
    /// doesn't update until motion is over.
    /// </summary>
    public BoundedNode2D MotionBox { get; private set; } = null;

    /// <summary>Move the unit along a path of map cells.  Cells should be contiguous.</summary>
    /// <param name="path">Coordinates of the cells to move along.</param>
    public void MoveAlong(List<Vector2I> path)
    {
        if (path.Count > 0)
        {
            foreach (Vector2I cell in path)
                Path.Curve.AddPoint(Grid.PositionOf(cell) - Position);
            Cell = path.Last();
            _tree.Set(Selected, false);
            _tree.Set(Moving, true);
            SetProcess(true);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        _tree = GetNode<AnimationTree>("AnimationTree");

        _sprite = GetNode<Sprite2D>("Path/PathFollow/Sprite");
        if (_affiliation is not null)
            _sprite.Modulate = _affiliation.Color;

        if (!Engine.IsEditorHint())
        {
            Path.Curve = new();
            MotionBox = GetNode<BoundedNode2D>("Path/PathFollow/Bounds");
            MotionBox.Size = Size;
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            Vector2 prev = PathFollow.Position;
            PathFollow.Progress += (float)(MoveSpeed*delta);
            Vector2 change = PathFollow.Position - prev;
            if (change != Vector2.Zero)
                _tree.Set(MoveDirection, change);

            if (PathFollow.ProgressRatio >= 1)
            {
                _tree.Set(Selected, true);
                _tree.Set(Moving, false);
                PathFollow.Progress = 0;
                Position = Grid.PositionOf(Cell);
                Path.Curve.ClearPoints();
                SetProcess(false);
                EmitSignal(SignalName.DoneMoving);
            }
        }
        else
        {
            if (_affiliation is not null)
                _sprite.Modulate = _affiliation.Color;
        }
    }
}