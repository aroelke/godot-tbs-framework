using Godot;
using level.manager;

namespace level.ui;

/// <summary>Cursor on the grid used for highlighting a cell and selecting things in it.</summary>
public partial class Cursor : Sprite2D, ILevelManaged
{
    private LevelManager _levelManager;
    private Vector2I _cell = Vector2I.Zero;

    public LevelManager LevelManager => _levelManager ??= GetParent<LevelManager>();

    /// <summary>Grid cell the cursor occupies. Is always inside the grid managed by the <c>LevelManager</c>.</summary>
    public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I next = LevelManager.Clamp(value);
            if (next != _cell)
            {
                _cell = next;
                Position = LevelManager.PositionOf(_cell);
            }
        }
    }

    /// <summary>Update the grid cell when the pointer signals it has moved.</summary>
    /// <param name="previous">Previous position of the pointer.</param>
    /// <param name="current">Next position of the pointer.</param>
    public void OnPointerMoved(Vector2 previous, Vector2 current) => Cell = LevelManager.CellOf(current);
}