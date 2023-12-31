using Godot;
using level.map;

namespace level.Object;

/// <summary>A node representing an object that moves on a grid.</summary>
[GlobalClass]
public partial class GridNode : Node2D
{
    /// <summary>Signals that the cell containing the object has changed.</summary>
    /// <param name="cell">New cell containing the object.</param>
    [Signal] public delegate void CellChangedEventHandler(Vector2I cell);

    private Vector2I _cell = Vector2I.Zero;

    /// <summary>Grid on which the containing object sits.</summary>
    [Export] public LevelMap Grid;

    /// <summary>Cell on the grid that this object currently occupies.</summary>
    [Export] public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I next = Grid.Clamp(value);
            if (next != _cell)
            {
                _cell = next;
                EmitSignal(SignalName.CellChanged, _cell);
            }
        }
    }
}