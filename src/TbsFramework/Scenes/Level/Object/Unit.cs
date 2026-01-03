using System.Collections.Generic;
using System.Linq;
using Godot;
using System;
using TbsFramework.Data;
using TbsFramework.Extensions;
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
public partial class Unit : GridNode, IHasHealth
{
    /// <summary>Signal that the unit is done moving along its path.</summary>
    [Signal] public delegate void DoneMovingEventHandler();

    private readonly NodeCache _cache = null;
    private UnitMapAnimations _animations = null;
    private Army _army = null;
    private Vector2I _target = Vector2I.Zero;

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

            Health.Connect<double>(HealthComponent.SignalName.MaximumChanged, _animations.SetHealthMax);
            Health.Connect(HealthComponent.SignalName.ValueChanged, new Callable(_animations, UnitMapAnimations.MethodName.SetHealthValue));
            _animations.SetHealthMax(Health.Maximum);
            _animations.SetHealthValue(Health.Value);
        }
    }

    private (IEnumerable<Vector2I>, IEnumerable<Vector2I>, IEnumerable<Vector2I>) ExcludeOccupants(IEnumerable<Vector2I> move, IEnumerable<Vector2I> attack, IEnumerable<Vector2I> support)
    {
        IEnumerable<Unit> allies = Grid.Occupants.Select(static (e) => e.Value).OfType<Unit>().Where(Army.Faction.AlliedTo);
        IEnumerable<Unit> enemies = Grid.Occupants.Select(static (e) => e.Value).OfType<Unit>().Where((u) => !Army.Faction.AlliedTo(u));
        return (
            move.Where((c) => !enemies.Any((u) => u.Cell == c)),
            attack.Where((c) => !allies.Any((u) => u.Cell == c)),
            support.Where((c) => !enemies.Any((u) => u.Cell == c))
        );
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

    ///<summary>Behavior defining actions to take when AI controlled.</summary>
    public Behavior Behavior
    {
        get => UnitData.Behavior;
        set => UnitData.Behavior = value;
    }

    public HealthComponent Health => _cache.GetNodeOrNull<HealthComponent>("Health");

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

    public Faction Faction => UnitData.Faction;

    /// <summary>Whether or not the unit has completed its turn.</summary>
    public bool Active
    {
        get => UnitData.Active;
        set => UnitData.Active = value;
    }

    /// <summary>Whether or not the unit is currently moving along a path.</summary>
    public bool IsMoving => IsProcessing();

    /// <summary>Box defining the unit's current position and size, in pixels on the battlefield, including during movement.</summary>
    public BoundedNode2D MotionBox => _cache.GetNode<BoundedNode2D>("Path/Follow/MotionBox");

    public Unit() : base() { _cache = new(this); }

    /// <returns>The set of cells that this unit can reach from its position, accounting for <see cref="Terrain.Cost"/>.</returns>
    public IEnumerable<Vector2I> TraversableCells() => UnitData.GetTraversableCells();

    public IEnumerable<Vector2I> AttackableCells(IGrid grid, IEnumerable<Vector2I> sources) => sources.SelectMany((c) => UnitData.GetAttackableCells(c)).ToHashSet();

    /// <summary>Compute all of the cells this unit could attack from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute attack range from.</param>
    /// <returns>The set of all cells that could be attacked from any of the cell <paramref name="sources"/>.</returns>
    public IEnumerable<Vector2I> AttackableCells(IEnumerable<Vector2I> sources) => AttackableCells(Grid, sources);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> AttackableCells(Vector2I source) => AttackableCells([source]);

    /// <inheritdoc cref="AttackableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> AttackableCells() => AttackableCells(Cell);

    public IEnumerable<Vector2I> SupportableCells(IGrid grid, IEnumerable<Vector2I> sources) => sources.SelectMany((c) => UnitData.GetSupportableCells(c)).ToHashSet();

    /// <summary>Compute all of the cells this unit could support from the given set of source cells.</summary>
    /// <param name="sources">Cells to compute support range from.</param>
    /// <returns>The set of all cells that could be supported from any of the source cells.</returns>
    public IEnumerable<Vector2I> SupportableCells(IEnumerable<Vector2I> sources) => SupportableCells(Grid, sources);

    /// <inheritdoc cref="SupportableCells"/>
    /// <remarks>Uses a singleton set of cells constructed from the single <paramref name="source"/> cell.</remarks>
    public IEnumerable<Vector2I> SupportableCells(Vector2I source) => SupportableCells([source]);

    /// <inheritdoc cref="SupportableCells"/>
    /// <remarks>Uses the unit's current <see cref="Cell"/> as the source.</remarks>
    public IEnumerable<Vector2I> SupportableCells() => SupportableCells(Cell);

    /// <returns>The complete sets of cells this unit can act on.</returns>
    public (IEnumerable<Vector2I> traversable, IEnumerable<Vector2I> attackable, IEnumerable<Vector2I> supportable) ActionRanges()
    {
        IEnumerable<Vector2I> traversable = TraversableCells();
        return ExcludeOccupants(
            traversable,
            AttackableCells(traversable.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == this)),
            SupportableCells(traversable.Where((c) => !Grid.Occupants.ContainsKey(c) || Grid.Occupants[c] == this))
        );
    }

    /// <summary>Put the unit in the "selected" state.</summary>
    public void Select() => _animations.PlaySelected();

    /// <summary>Put the unit in the "idle" state.</summary>
    public void Deselect() => _animations.PlayIdle();

    /// <summary>Put the unit in its "done" state, indicating it isn't available to act anymore.</summary>
    public void Finish()
    {
        UnitData.Active = false;
        _animations.Modulate = Colors.White;
        _animations.PlayDone();
    }

    /// <summary>Restore the unit into its "idle" state from being inactive, indicating that it's ready to act again.</summary>
    public void Refresh()
    {
        UnitData.Active = true;
        if (Faction is null || !UnitData.Class.MapAnimationsPaths.ContainsKey(Faction))
            _animations.Modulate = Faction?.Color ?? Colors.White;
        _animations.PlayIdle();
    }

    /// <summary>Remove the unit from the map and delete it.</summary>
    public void Die()
    {
        UnitData.Grid.Occupants.Remove(Cell);
        Grid.Occupants.Remove(Cell);
        QueueFree();
    }

    /// <summary>Move the unit along a path of <see cref="Grid"/> cells.</summary>
    /// <param name="path">Coordinates of the cells to move along.</param>
    public void MoveAlong(Path path)
    {
        if (path[0] != Cell)
            throw new ArgumentException("The first cell in the path must be the unit's cell");

        if (path.Count == 1)
            EmitSignal(SignalName.DoneMoving);
        else if (path.Count > 1)
        {
            foreach (Vector2I cell in path)
                Path.Curve.AddPoint(Grid.PositionOf(cell) - Position);
            _target = path[^1];
            SetProcess(true);
        }
    }

    /// <summary>If this unit is moving, skip straight to the end of the path.</summary>
    /// <exception cref="InvalidOperationException">If the unit is not moving.</exception>
    public void SkipMoving()
    {
        if (!IsMoving)
            throw new InvalidOperationException($"Unit {Name} isn't moving");
        PathFollow.ProgressRatio = 1;
    }

    public void OnHealthChanged(int value)
    {
        if (value == 0)
            LevelEvents.Singleton.EmitSignal(LevelEvents.SignalName.UnitDefeated, this);
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

        Behavior = GetChildren().OfType<Behavior>().FirstOrDefault();
        UnitData.Renderer = this;

        if (UnitData.Class is not null)
            UpdateVisuals(UnitData.Class, Faction);
        if (!Engine.IsEditorHint())
        {
            RemoveChild(EditorSprite);
            EditorSprite.QueueFree();
            _animations.PlayIdle();

            Path.Curve = new();
            MotionBox.Size = Size;
            SetProcess(false);
        }

        Health.Value = Health.Maximum = Stats.Health;

        UnitData.FactionUpdated += (faction) => {
            if (UnitData.Class is not null)
                UpdateVisuals(UnitData.Class, faction);
        };
        UnitData.ClassUpdated += (@class) => {
            if (@class is not null)
                UpdateVisuals(@class, Faction);
        };
        UnitData.StatsUpdated += (stats) => {
            if (Health is not null)
                Health.Maximum = stats.Health;
        };
        UnitData.HealthUpdated += (hp) => Health.Value = hp;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint())
        {
            if (UnitData.Class is not null && EditorSprite.Texture is null)
                UpdateVisuals(UnitData.Class, Faction);
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
                _animations.PlaySelected();
                PathFollow.Progress = 0;
                Cell = _target;
                Path.Curve.ClearPoints();
                SetProcess(false);
                EmitSignal(SignalName.DoneMoving);
            }
        }
    }
}