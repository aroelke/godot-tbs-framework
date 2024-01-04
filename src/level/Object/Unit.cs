using System.Collections.Generic;
using System.Linq;
using Godot;
using level.Object.Group;

namespace level.Object;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and interface for the player to
/// interact.
/// </summary>
public partial class Unit : GridNode
{
    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private Path2D _path = null;
    private PathFollow2D _follow = null;
    private AnimationPlayer _animation = null;
    private bool _selected = false;
    private bool _moving = false;

    private Path2D Path => _path = GetNode<Path2D>("Path");
    private PathFollow2D PathFollow => _follow = GetNode<PathFollow2D>("Path/PathFollow");
    private AnimationPlayer Animation => _animation = GetNode<AnimationPlayer>("Animation");

    /// <summary>Movement range of the unit, in grid cells.</summary>
    [Export] public int MoveRange = 5;

    /// <summary>Distances from the unit's occupied cell that it can attack.</summary>
    [Export] public int[] AttackRange = new[] { 1, 2 };

    /// <summary>Distances from the unit's occupied cell that it can support.</summary>
    [Export] public int[] SupportRange = new[] { 1, 2, 3 };

    /// <summary>Army to which this unit belongs, to determine its allies and enemies.</summary>
    [Export] public Army Affiliation = null;

    /// <summary>Speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

    /// <summary>Whether or not this unit has been selected and is awaiting instruction. Changing it toggles the selected animation.</summary>
    public bool IsSelected
    {
        get => _selected;
        set
        {
            _selected = value;
            if (Animation is not null)
            {
                if (_selected)
                    Animation.Play("selected");
                else
                    Animation.Play("idle");
            }
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
    public void MoveAlong(List<Vector2I> path)
    {
        if (path.Count > 0)
        {
            foreach (Vector2I cell in path)
                Path.Curve.AddPoint(Grid.PositionOf(cell) - Position);
            Cell = path.Last();
            IsMoving = true;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
            Path.Curve = new();
        
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 prev = PathFollow.Position;
        PathFollow.Progress += (float)(MoveSpeed*delta);
        string animation = (PathFollow.Position - prev) switch
        {
            Vector2(_, <0) => "up",
            Vector2(<0, _) => "left",
            Vector2(_, >0) => "down",
            Vector2(>0, _) => "right",
            _ => "idle"
        };
        if (Animation.CurrentAnimation != animation)
            Animation.Play(animation);

        if (PathFollow.ProgressRatio >= 1)
        {
            IsMoving = false;
            PathFollow.Progress = 0;
            Position = Grid.PositionOf(Cell);
            Path.Curve.ClearPoints();
            IsSelected = _selected; // Go back to standing animation (idle/selected)
            EmitSignal(SignalName.DoneMoving);
        }
    }
}