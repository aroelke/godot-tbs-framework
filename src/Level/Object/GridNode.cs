using System;
using System.Collections.Generic;
using Godot;
using Level.Map;

namespace Level.Object;

/// <summary>A node representing an object that moves on a grid.</summary>
[GlobalClass, Tool]
public partial class GridNode : Node2D
{
    /// <summary>Signals that the cell containing the object has changed.</summary>
    /// <param name="cell">New cell containing the object.</param>
    [Signal] public delegate void CellChangedEventHandler(Vector2I cell);

    private Vector2I _cell = Vector2I.Zero;

    /// <summary>Grid on which the containing object sits.</summary>
    [Export] public Grid Grid;

    /// <summary>Cell on the grid that this object currently occupies.</summary>
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

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        if (Grid == null)
            warnings.Add("No grid to move on has been defined.");
        else if (Cell.X < 0 || Cell.Y < 0 || Cell.X >= Grid.Size.X || Cell.Y >= Grid.Size.Y)
            warnings.Add("Outside grid bounds.");

        return warnings.ToArray();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint() && Grid is not null)
            Cell = Grid.CellOf(Position);
    }
}