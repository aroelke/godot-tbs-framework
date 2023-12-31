using System.Collections.Generic;
using System.Linq;
using Godot;
using level.Object.Component;
using level.unit;

namespace level.Object;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and interface for the player to
/// interact.
/// </summary>
public partial class Unit : Node2D
{
    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private bool _selected = false;
    private bool _moving = false;

    /// <summary>Grid object component maintaining cell occupancy.</summary>
    [ExportGroup("Components")]
    [Export] public GridObject GridObject { get; private set; } = null;

    /// <summary>Path component maintaining the move path.</summary>
    [ExportGroup("Components")]
    [Export] public Path2D Path { get; private set; } = null;

    /// <summary>Component controlling (the illusion of) movement along the move path.</summary>
    [ExportGroup("Components")]
    [Export] public PathFollow2D PathFollow { get; private set; } = null;

    /// <summary>Animation controller component.</summary>
    [ExportGroup("Components")]
    [Export] public AnimationPlayer Animation { get; private set; } = null;

    /// <summary>Movement range of the unit, in grid cells.</summary>
    [ExportGroup("Stats")]
    [Export] public int MoveRange = 5;

    /// <summary>Distances from the unit's occupied cell that it can attack.</summary>
    [ExportGroup("Stats")]
    [Export] public int[] AttackRange = new[] { 1, 2 };

    /// <summary>Distances from the unit's occupied cell that it can support.</summary>
    [ExportGroup("Stats")]
    [Export] public int[] SupportRange = new[] { 1, 2, 3 };

    /// <summary>Army to which this unit belongs, to determine its allies and enemies.</summary>
    [Export] public ArmyManager Affiliation = null;

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
                Path.Curve.AddPoint(GridObject.Grid.PositionOf(cell) - Position);
            GridObject.Cell = path.Last();
            IsMoving = true;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        GridObject.Cell = GridObject.Grid.CellOf(Position);
        Position = GridObject.Grid.PositionOf(GridObject.Cell);

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
            Position = GridObject.Grid.PositionOf(GridObject.Cell);
            Path.Curve.ClearPoints();
            IsSelected = _selected; // Go back to standing animation (idle/selected)
            EmitSignal(SignalName.DoneMoving);
        }
    }
}