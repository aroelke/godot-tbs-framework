using System;
using System.Linq;
using Godot;
using TbsTemplate.Nodes;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.State.Occupants;

namespace TbsTemplate.Scenes.Level.Object;

[SceneTree, Tool]
public partial class UnitRenderer : BoundedNode2D
{
    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    // AnimationTree parameters
    private static readonly StringName Idle = "parameters/conditions/idle";
    private static readonly StringName Selected = "parameters/conditions/selected";
    private static readonly StringName Moving = "parameters/conditions/moving";
    private static readonly StringName MoveDirection = "parameters/Moving/Walking/blend_position";
    private static readonly StringName Done = "parameters/conditions/done";

    // AnimationTree states
    private static readonly StringName IdleState = "Idle";
    private static readonly StringName SelectedState = "Selected";
    private static readonly StringName MovingState = "Moving";
    private static readonly StringName DoneState = "Done";
    private StringName AnimationState => AnimationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>().GetCurrentNode();

    private Vector2I _target = Vector2I.Zero;
    private GridRenderer _grid = null;

    private void CheckInvalidState(string operation, params StringName[] illegal)
    {
        if (illegal.Contains(AnimationState))
            throw new InvalidOperationException($"Cannot {operation} unit {Name} while in animation state {AnimationState}");
    }

    private void CheckValidState(string operation, params StringName[] legal)
    {
        if (!legal.Contains(AnimationState))
            throw new InvalidOperationException($"Cannot {operation} unit {Name} while in animation state {AnimationState}");
    }

    [Export] public UnitState State = new();

    [Export] public GridRenderer Grid
    {
        get
        {
            if (_grid is not null && _grid.State != State.Grid)
                throw new InvalidOperationException("GridRenderer.State and State.Grid do not match");
            return _grid;
        }
        set
        {
            _grid = value;
            if (_grid is not null)
            {
                State.Grid = _grid.State;
                _grid.State.SetOccupant(State.Cell, State);
            }
        }
    }

    /// <summary>Base speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

    /// <summary>Factor to multiply <see cref="MoveSpeed"/> by while <see cref="MoveAccelerateAction"/> is held down.</summary>
    [Export] public double MoveAccelerationFactor = 2;

    /// <summary>Whether or not the unit has completed its turn.</summary>
    public bool Active => !AnimationTree.Get(Done).AsBool();

    /// <summary>Put the unit in the "selected" state.</summary>
    public void Select()
    {
        CheckInvalidState("select", DoneState);
        AnimationTree.Set(Idle, false);
        AnimationTree.Set(Selected, true);
        AnimationTree.Set(Moving, false);
    }

    /// <summary>Put the unit in the "idle" state.</summary>
    public void Deselect()
    {
        CheckInvalidState("deselect", IdleState);
        AnimationTree.Set(Idle, true);
        AnimationTree.Set(Selected, false);
        AnimationTree.Set(Moving, false);
    }

    /// <summary>Put the unit in its "done" state, indicating it isn't available to act anymore.</summary>
    public void Finish()
    {
        Sprite.Modulate = Colors.White;
        AnimationTree.Set(Idle, false);
        AnimationTree.Set(Selected, false);
        AnimationTree.Set(Moving, false);
        AnimationTree.Set(Done, true);
    }

    /// <summary>Restore the unit into its "idle" state from being inactive, indicating that it's ready to act again.</summary>
    public void Refresh()
    {
        CheckValidState("refresh", "", DoneState);
        Sprite.Modulate = State.Faction.Color;
        AnimationTree.Set(Done, false);
        AnimationTree.Set(Idle, true);
    }

    /// <summary>Play the unit's death animation and then remove it from the scene.</summary>
    public void Die()
    {
        State.Grid.RemoveOccupant(State.Cell);
        QueueFree();
    }

    /// <summary>Move the unit along a path of <see cref="Grid"/> cells.</summary>
    /// <param name="path">Coordinates of the cells to move along.</param>
    public void MoveAlong(Path path)
    {
        if (path[0] != State.Cell)
            throw new ArgumentException("The first cell in the path must be the unit's cell");

        if (path.Count == 1)
            EmitSignal(SignalName.DoneMoving);
        else if (path.Count > 1)
        {
            foreach (Vector2I cell in path)
                Path.Curve.AddPoint(Grid.PositionOf(cell) - Position);
            AnimationTree.Set(Selected, false);
            AnimationTree.Set(Moving, true);
            _target = path[^1];
            SetProcess(true);
        }
    }

    /// <summary>If this unit is moving, skip straight to the end of the path.</summary>
    /// <exception cref="InvalidOperationException">If the unit is not moving.</exception>
    public void SkipMoving()
    {
        if (!AnimationTree.Get(Moving).AsBool())
            throw new InvalidOperationException($"Unit {Name} isn't moving");
        PathFollow.ProgressRatio = 1;
    }

    public override void _Ready()
    {
        base._Ready();

        if (State?.Faction is not null)
                Sprite.Modulate = State.Faction.Color;

        if (!Engine.IsEditorHint())
        {
            State.Health.ValueChanged += (v) => {
                if (v == 0)
                    LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitDefeated, this);
            };
            State.Health.ValueChanged += HealthBar.SetValue;
            State.Health.MaximumChanged += HealthBar.SetMax;

            Path.Curve = new();
            MotionBox.Size = Size;
            AnimationTree.Active = true;
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Engine.IsEditorHint())
        {
            Vector2 prev = PathFollow.Position;
            PathFollow.Progress += (float)(MoveSpeed*(Accelerate.Active ? MoveAccelerationFactor : 1)*delta);
            Vector2 change = PathFollow.Position - prev;
            if (change != Vector2.Zero)
                AnimationTree.Set(MoveDirection, change);

            if (PathFollow.ProgressRatio >= 1)
            {
                AnimationTree.Set(Selected, true);
                AnimationTree.Set(Moving, false);
                PathFollow.Progress = 0;
                State.Cell = _target;
                Path.Curve.ClearPoints();
                SetProcess(false);
                EmitSignal(SignalName.DoneMoving);
            }
        }
        else
        {
            if (State?.Faction is not null)
                Sprite.Modulate = State.Faction.Color;
        }
    }
}