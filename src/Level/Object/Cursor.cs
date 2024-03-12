using Godot;
using UI.Controls.Action;
using UI.Controls.Device;

namespace Level.Object;

/// <summary>
/// Cursor on the grid used for highlighting a cell and selecting things in it.  Importantly, it does not move itself;
/// rather, it emits signals to a controller (possibly a <c>PointerProjection</c>) to move it when digital movement
/// is desired.
/// </summary>
[Tool]
public partial class Cursor : GridNode
{
    /// <summary>Emitted when the cursor moves to a new cell.</summary>
    /// <param name="cell">Position of the center of the cell moved to.</param>
    [Signal] public delegate void CursorMovedEventHandler(Vector2 position);

    /// <summary>Signals that a cell has been selected.</summary>
    /// <param name="cell">Coordinates of the cell that has been selected.</param>
    [Signal] public delegate void CellSelectedEventHandler(Vector2I cell);

    /// <summary>
    /// Signals that the cursor wants to skip toward a direction. Where it goes depends on the context. On the field, it means skipping to the
    /// next region edge (traversable cells if a unit is selected and the cursor is in that region or map edge otherwise).
    /// </summary>
    /// <param name="direction">Direction to skip.</param>
    [Signal] public delegate void RequestSkipEventHandler(Vector2I direction);

    /// <summary>Action for selecting a cell.</summary>
    [Export] public InputActionReference SelectAction = new();

    private DigitalMoveAction _mover = null;
    private DigitalMoveAction MoveController => _mover ??= GetNode<DigitalMoveAction>("MoveController");

    /// <summary>Cell the cursor occupies. Overrides <c>GridNode.Cell</c> to ensure that the position is updated before any signals fire.</summary>
    public override Vector2I Cell
    {
        get => base.Cell;
        set
        {
            if (Engine.IsEditorHint() && Grid is null)
                base.Cell = value;
            else
            {
                Vector2I next = Grid.Clamp(value);
                if (next != Cell)
                {
                    Position = Grid.PositionOf(next);
                    base.Cell = next;
                    EmitSignal(SignalName.CursorMoved, Position + Grid.CellSize/2);
                }
            }
        }
    }

    /// <summary>Briefly break continuous digital movement to allow reaction from the player (e.g. the cursor has reached the edge of the movement range).</summary>
    public void BreakMovement() => MoveController.ResetEcho();

    /// <summary>When a direction is pressd, move the cursor to the adjacent cell there.</summary>
    public void OnDirectionPressed(Vector2I direction) => Cell += direction;

    /// <summary>Update the grid cell when the pointer signals it has moved, unless the cursor is what's controlling movement.</summary>
    /// <param name="position">Position of the pointer.</param>
    public void OnPointerMoved(Vector2 position)
    {
        if (DeviceManager.Mode != InputMode.Digital)
            Cell = Grid.CellOf(position);
    }

    /// <summary>Forward skips to the level manager so it can determine where the skip should end up.</summary>
    /// <param name="direction">Direction to skip.</param>
    public void OnSkip(Vector2I direction) => EmitSignal(SignalName.RequestSkip, direction);

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event.IsActionReleased(SelectAction))
            EmitSignal(SignalName.CellSelected, Grid.CellOf(Position));
    }
}