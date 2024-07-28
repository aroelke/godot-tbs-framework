using System;
using System.Collections.Generic;
using Godot;
using Nodes;
using Scenes.Level.Map;

namespace Scenes.Level.Object;

/// <summary>A node representing an object that moves on a <see cref="Map.Grid"/>.</summary>
[Icon("res://icons/GridNode.svg"), GlobalClass, Tool]
public partial class GridNode : BoundedNode2D
{
    /// <summary>Signals that the cell containing the object has changed.</summary>
    /// <param name="cell">New cell containing the object.</param>
    [Signal] public delegate void CellChangedEventHandler(Vector2I cell);

    private Vector2I _cell = Vector2I.Zero;

    /// <summary>Grid on which the containing object sits.</summary>
    [Export] public Grid Grid;

    /// <summary>Cell on the <see cref="Map.Grid"/> that this object currently occupies.</summary>
    [Export] public virtual Vector2I Cell
    {
        get => _cell;
        set
        {
            if (Engine.IsEditorHint())
            {
                if (_cell != value)
                {
                    _cell = value;
                    UpdateConfigurationWarnings();
                }
            }
            else
            {
                Vector2I next = Grid?.Clamp(value) ?? value;
                if (next != _cell)
                {
                    _cell = next;
                    EmitSignal(SignalName.CellChanged, _cell);
                }
            }
        }
    }

    /// <inheritdoc cref="BoundedNode2D.Size"/>
    /// <remarks>Grid nodes have a constant size that is based on the size of the <see cref="Map.Grid"/> cells.</remarks>
    public override Vector2 Size { get => Grid?.CellSize ?? Vector2.Zero; set {}}

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Grid is null)
            warnings.Add("No grid to move on has been defined.");
        else if (Cell.X < 0 || Cell.Y < 0 || Cell.X >= Grid.Size.X || Cell.Y >= Grid.Size.Y)
            warnings.Add("Outside grid bounds.");

        return [.. warnings];
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint() && Grid is not null)
            Cell = Grid.CellOf(Position);
    }
}