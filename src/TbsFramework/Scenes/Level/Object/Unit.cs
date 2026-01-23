using System.Collections.Generic;
using System.Linq;
using Godot;
using System;
using TbsFramework.Data;
using TbsFramework.Nodes.Components;
using TbsFramework.Scenes.Level.Map;
using TbsFramework.Scenes.Level.Object.Group;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Level.Control;
using TbsFramework.Nodes;

namespace TbsFramework.Scenes.Level.Object;

/// <summary>
/// A unit that moves around the map.  Mostly is just a visual representation of what's where and an interface for the player to
/// interact.
/// </summary>
[GlobalClass, Tool]
public partial class Unit : GridNode
{
    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private readonly NodeCache _cache = null;
    private UnitMapAnimations _animations = null;
    private Army _army = null;
    private Vector2I _target = -Vector2I.One;

    private Sprite2D             EditorSprite   => _cache.GetNode<Sprite2D>("EditorSprite");
    private FastForwardComponent Accelerate     => _cache.GetNode<FastForwardComponent>("Accelerate");
    private Path2D               Path           => _cache.GetNode<Path2D>("Path");
    private PathFollow2D         PathFollow     => _cache.GetNode<PathFollow2D>("Path/Follow");

    private void UpdateVisuals(Class @class, Faction faction)
    {
        if (Engine.IsEditorHint())
        {
            if (faction is not null && @class.EditorSprites.TryGetValue(faction, out Texture2D texture))
                EditorSprite.Texture = texture;
            else
            {
                EditorSprite.Texture = @class.DefaultEditorSprite;
                EditorSprite.Modulate = faction?.Color ?? Colors.White;
            }
        }
        else
        {
            _animations?.QueueFree();
            _animations = @class.InstantiateMapAnimations(faction);
            GetNode<PathFollow2D>("Path/Follow").AddChild(_animations);

            _animations.SetHealthMax(UnitData.Stats.Health);
            _animations.SetHealthValue(UnitData.Health);
        }
    }

    /// <summary>
    /// If this unit is moved to the end of its movement path while moving, it assumes it was meant to skip the rest of its movement
    /// animation and stops moving.
    /// </summary>
    private void OnMoveSkipped(Vector2I from, Vector2I to)
    {
        if (IsMoving && to == _target)
            PathFollow.ProgressRatio = 1;
    }

    private void OnAvailabilityUpdated(bool active)
    {
        if (active)
            Refresh();
        else
            Finish();
    }

    private void OnFactionUpdated(Faction _, Faction faction)
    {
        if (UnitData.Class is not null)
            UpdateVisuals(UnitData.Class, faction);
    }

    private void OnClassUpdated(Class _, Class @class)
    {
        if (@class is not null)
            UpdateVisuals(@class, UnitData.Faction);
    }

    private void OnStatsUpdated(Stats stats) => _animations?.SetHealthMax(stats.Health);

    private void OnHealthUpdated(double _, double hp)
    {
        _animations?.SetHealthValue(hp);
        if (hp == 0)
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitDefeated, this);
    }

    public UnitData UnitData { get; init; } = new();
    public override GridObjectData Data => UnitData;

    /// <summary>Class this unit belongs to, defining some of its stats and animations.</summary>
    [Export] public Class Class
    {
        get => UnitData.Class;
        set => UnitData.Class = value;
    }

    [Export] public Stats Stats
    {
        get => UnitData.Stats;
        set => UnitData.Stats = value;
    }

    [ExportGroup("Path Traversal", "Move")]

    /// <summary>Base speed, in world pixels/second, to move along the path while moving.</summary>
    [Export] public double MoveSpeed = 320;

    /// <summary>Factor to multiply <see cref="MoveSpeed"/> by while <see cref="MoveAccelerateAction"/> is held down.</summary>
    [Export] public double MoveAccelerationFactor = 2;

    /// <summary>Army to which this unit belongs, which determines its alliances and gives access to its compatriots.</summary>
    public Army Army
    {
        get => _army;
        set
        {
            if (_army != value)
            {
                _army = value;
                UnitData.Faction = _army.Faction;
            }
        }
    }

    /// <summary>Whether or not the unit is currently moving along a path.</summary>
    public bool IsMoving => IsProcessing();

    /// <summary>Box defining the unit's current position and size, in pixels on the battlefield, including during movement.</summary>
    public BoundedNode2D MotionBox => _cache.GetNode<BoundedNode2D>("Path/Follow/MotionBox");

    public Unit() : base() { _cache = new(this); }

    /// <summary>Put the unit in the "selected" state.</summary>
    public void Select() => _animations.PlaySelected();

    /// <summary>Put the unit in the "idle" state.</summary>
    public void Deselect() => _animations.PlayIdle();

    /// <summary>Put the unit in its "done" state, indicating it isn't available to act anymore.</summary>
    public void Finish()
    {
        _animations.Modulate = Colors.White;
        _animations.PlayDone();
    }

    /// <summary>Restore the unit into its "idle" state from being inactive, indicating that it's ready to act again.</summary>
    public void Refresh()
    {
        if (UnitData.Faction is null || !UnitData.Class.MapAnimationsPaths.ContainsKey(UnitData.Faction))
            _animations.Modulate = UnitData.Faction?.Color ?? Colors.White;
        _animations.PlayIdle();
    }

    /// <summary>Remove the unit from the map and delete it.</summary>
    public void Die()
    {
        UnitData.AvailabilityUpdated -= OnAvailabilityUpdated;
        UnitData.FactionUpdated      -= OnFactionUpdated;
        UnitData.ClassUpdated        -= OnClassUpdated;
        UnitData.StatsUpdated        -= OnStatsUpdated;
        UnitData.HealthUpdated       -= OnHealthUpdated;

        UnitData.Grid.Occupants.Remove(Data.Cell);
        QueueFree();
    }

    /// <summary>Move the unit along a path of <see cref="Grid"/> cells.</summary>
    /// <param name="path">Coordinates of the cells to move along.</param>
    public void MoveAlong(Path path)
    {
        if (path[0] != Data.Cell)
            throw new ArgumentException("The first cell in the path must be the unit's cell");

        // Assumes the path doesn't start and end on the unit's current cell, which shouldn't happen anyway
        Data.WhenDoneMoving(path[^1], () => EmitSignal(SignalName.DoneMoving));
        if (path.Count > 1)
        {
            _target = path[^1];
            foreach (Vector2I cell in path)
                Path.Curve.AddPoint(Grid.PositionOf(cell) - Position);
            Data.CellChanged += OnMoveSkipped;
            SetProcess(true);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (!GetChildren().OfType<Behavior>().Any() && (!GetParentOrNull<Army>()?.GetChildren().OfType<PlayerController>().Any() ?? false))
            warnings.Add("This unit has no behavior. It may not be able to act.");
        if (GetChildren().OfType<Behavior>().Count() > 1)
            warnings.Add("More than one behavior is defined. Only the first one will be used.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (UnitData.Class is not null)
            UpdateVisuals(UnitData.Class, UnitData.Faction);
        if (!Engine.IsEditorHint())
        {
            UnitData.Behavior = GetChildren().OfType<Behavior>().FirstOrDefault();
            UnitData.Renderer = this;

            RemoveChild(EditorSprite);
            EditorSprite.QueueFree();
            _animations.PlayIdle();

            Path.Curve = new();
            MotionBox.Size = Size;
            SetProcess(false);
        }

        UnitData.AvailabilityUpdated += OnAvailabilityUpdated;
        UnitData.FactionUpdated      += OnFactionUpdated;
        UnitData.ClassUpdated        += OnClassUpdated;
        UnitData.StatsUpdated        += OnStatsUpdated;
        UnitData.HealthUpdated       += OnHealthUpdated;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint())
        {
            if (UnitData.Class is not null && EditorSprite.Texture is null)
                UpdateVisuals(UnitData.Class, UnitData.Faction);
        }
        else
        {
            Vector2 prev = PathFollow.Position;
            PathFollow.Progress += (float)(MoveSpeed*(Accelerate.Active ? MoveAccelerationFactor : 1)*delta);
            Vector2 change = PathFollow.Position - prev;
            if (change != Vector2.Zero)
                _animations.PlayMove(change);

            if (PathFollow.ProgressRatio >= 1)
            {
                Data.CellChanged -= OnMoveSkipped;
                _animations.PlaySelected();
                PathFollow.Progress = 0;
                Path.Curve.ClearPoints();
                SetProcess(false);
                Data.Cell = _target;
                _target = -Vector2I.One;
            }
        }
    }
}