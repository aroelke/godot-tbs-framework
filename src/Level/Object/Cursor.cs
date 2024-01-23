using Godot;
using UI.Controls.Action;
using UI.Controls.Device;

namespace Level.Object;

/// <summary>
/// Cursor on the grid used for highlighting a cell and selecting things in it.  Importantly, it does not move itself;
/// rather, it emits signals to a controller (possibly a <c>PointerProjection</c>) to move it when digital movement
/// is desired.
/// </summary>
public partial class Cursor : GridNode
{
    /// <summary>Emitted when the cursor moves to a new cell.</summary>
    /// <param name="cell">Position of the center of the cell moved to.</param>
    [Signal] public delegate void CursorMovedEventHandler(Vector2 position);

    /// <summary>Signals that a cell has been selected.</summary>
    /// <param name="cell">Coordinates of the cell that has been selected.</param>
    [Signal] public delegate void CellSelectedEventHandler(Vector2I cell);

    private Timer _timer = null;
    private bool _echoing = false;
    private Vector2I _direction = Vector2I.Zero;

    private Timer EchoTimer => _timer = GetNode<Timer>("EchoTimer");

    /// <summary>Action for selecting a cell.</summary>
    [Export] public InputActionReference SelectAction = new();

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoInterval = 0.03;

    /// <summary>Cell the cursor occupies. Overrides <c>GridNode.Cell</c> to ensure that the position is updated before any signals fire.</summary>
    public override Vector2I Cell
    {
        get => base.Cell;
        set
        {
            Position = Grid.PositionOf(value);
            base.Cell = value;
            EmitSignal(SignalName.CursorMoved, Position + Grid.CellSize/2);
        }
    }

    /// <summary>When a direction is pressd, move the cursor to the adjacent cell there.</summary>
    public void OnDirectionPressed(Vector2I direction) => Cell += direction;

    /// <summary>Update the grid cell when the pointer signals it has moved, unless the cursor is what's controlling movement.</summary>
    /// <param name="position">Position of the pointer.</param>
    public void OnPointerMoved(Vector2 position)
    {
        if (DeviceManager.Mode != InputMode.Digital)
            Cell = Grid.CellOf(position);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event.IsActionReleased(SelectAction))
            EmitSignal(SignalName.CellSelected, Grid.CellOf(Position));
    }
}