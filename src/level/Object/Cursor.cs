using Godot;
using ui;
using ui.input;

namespace level.Object;

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

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [ExportGroup("Echo Control")]
    [Export] public double EchoInterval = 0.03;

    /// <summary>When the cell changes, update position, then convert to a grid cell center and notify listeners that the cursor moved.</summary>
    /// <param name="cell">Cell the cursor moved to.</param>
    public void OnCellChanged(Vector2I cell)
    {
        Position = Grid.PositionOf(cell);
        EmitSignal(SignalName.CursorMoved, Position + Grid.CellSize/2);
    }

    /// <summary>Update the grid cell when the pointer signals it has moved, unless the cursor is what's controlling movement.</summary>
    /// <param name="position">Position of the pointer.</param>
    public void OnPointerMoved(Vector2 position)
    {
        if (DeviceManager.Mode != InputMode.Digital)
            Cell = Grid.CellOf(position);
    }

    /// <summary>Start/continue echo movement of the cursor.</summary>
    public void OnEchoTimeout()
    {
        Cell += _direction;
        if (EchoInterval > GetProcessDeltaTime())
        {
            EchoTimer.WaitTime = EchoInterval;
            EchoTimer.Start();
        }
        else
            _echoing = true;
    }

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
                        Cell += dir - _direction;
                    else
                        Cell += dir;
                    _direction = dir;

                    EchoTimer.WaitTime = EchoDelay;
                    EchoTimer.Start();
                }
                else
                    _direction = Vector2I.Zero;
            }
        }
        if (@event.IsActionReleased("cursor_select"))
            EmitSignal(SignalName.CellSelected, Grid.CellOf(Position));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (DeviceManager.Mode == InputMode.Digital && _echoing)
            Cell += _direction;
    }
}