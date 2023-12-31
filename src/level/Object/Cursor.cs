using Godot;
using level.Object.Component;
using ui;
using ui.input;

namespace level.Object;

/// <summary>
/// Cursor on the grid used for highlighting a cell and selecting things in it.  Importantly, it does not move itself;
/// rather, it emits signals to a controller (possibly a <c>PointerProjection</c>) to move it when digital movement
/// is desired.
/// </summary>
public partial class Cursor : Sprite2D
{
    /// <summary>Emitted when the cursor moves to a new cell.</summary>
    /// <param name="cell">Position of the center of the cell moved to.</param>
    [Signal] public delegate void CursorMovedEventHandler(Vector2 position);

    /// <summary>Signals that a cell has been selected.</summary>
    /// <param name="cell">Coordinates of the cell that has been selected.</param>
    [Signal] public delegate void CellSelectedEventHandler(Vector2I cell);

    private bool _echoing = false;
    private Vector2I _direction = Vector2I.Zero;

    /// <summary>Projection of the pointer in the viewport onto the world.</summary>
    [Export] public PointerProjection Projection = null;

    [ExportGroup("Components")]
    [Export] public GridObject GridObject { get; private set; } = null;

    [ExportGroup("Components")]
    [Export] public Timer EchoTimer { get; private set; } = null;

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoInterval = 0.03;

    public void OnCellChanged(Vector2I cell)
    {
        Position = GridObject.Manager.PositionOf(cell);
        EmitSignal(SignalName.CursorMoved, Position + GridObject.Manager.CellSize/2);
    }

    /// <summary>Update the grid cell when the pointer signals it has moved, unless the cursor is what's controlling movement.</summary>
    /// <param name="viewport">Position of the point in the viewport.</param>
    /// <param name="world">Position of the pointer in the world.</param>
    public void OnPointerMoved(Vector2 viewport, Vector2 world)
    {
        if (DeviceManager.Mode != InputMode.Digital)
            GridObject.Cell = GridObject.Manager.CellOf(world);
    }

    /// <summary>Start/continue echo movement of the cursor.</summary>
    public void OnEchoTimeout()
    {
        GridObject.Cell += _direction;
        if (EchoInterval > GetProcessDeltaTime())
        {
            EchoTimer.WaitTime = EchoInterval;
            EchoTimer.Start();
        }
        else
            _echoing = true;
    }

    /// <summary>When the pointer is clicked, signal that a cell has been selected.</summary>
    /// <param name="viewport">Position of the pointer in the viewport.</param>
    /// <param name="world">Position of the pointer in the world.</param>
    public void OnPointerClicked(Vector2 viewport, Vector2 world) => EmitSignal(SignalName.CellSelected, GridObject.Manager.CellOf(world));

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (DeviceManager.Mode == InputMode.Digital)
        {
            Vector2I dir = InputManager.GetDigitalVector();
            if (dir != _direction)
            {
                EchoTimer.Stop();
                _echoing = false;

                if (dir != Vector2I.Zero)
                {
                    if (dir.Abs().X + dir.Abs().Y > _direction.Abs().X + _direction.Abs().Y)
                        GridObject.Cell += dir - _direction;
                    else
                        GridObject.Cell += dir;
                    _direction = dir;

                    EchoTimer.WaitTime = EchoDelay;
                    EchoTimer.Start();
                }
                else
                    _direction = Vector2I.Zero;
            }

            if (Input.IsActionJustReleased("cursor_select"))
                EmitSignal(SignalName.CellSelected, GridObject.Manager.CellOf(Position));
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (DeviceManager.Mode == InputMode.Digital && _echoing)
            GridObject.Cell += _direction;
    }
}