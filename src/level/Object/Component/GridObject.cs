using Godot;
using level.map;

namespace level.Object.Component;

/// <summary>
/// Component of an object on a grid that maintains its grid location. That object's position is not automatically updated when
/// the grid cell changes; rather, it should listen for <c>CellChanged</c> and update it there.
/// </summary>
public partial class GridObject : Node
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