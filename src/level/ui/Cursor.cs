using Godot;
using level.manager;
using ui.input;

namespace level.ui;

/// <summary>
/// Cursor on the grid used for highlighting a cell and selecting things in it.  Importantly, it does not move itself;
/// rather, it emits signals to a controller (possibly a <c>PointerProjection</c>) to move it when digital movement
/// is desired.
/// </summary>
public partial class Cursor : Sprite2D, ILevelManaged
{
    /// <summary>Emitted when a direction is pressed to request movement in that direction.</summary>
    /// <param name="position">Center of the cell to move to.</param>
    [Signal] public delegate void DirectionPressedEventHandler(Vector2 position);

    private InputManager _inputManager = null;
    private LevelManager _levelManager = null;
    private Timer _echo = null;
    private bool _echoing = false;
    private Vector2I _cell = Vector2I.Zero;
    private Vector2I _direction = Vector2I.Zero;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");
    private Timer EchoTimer => _echo ??= GetNode<Timer>("EchoTimer");

    /// <summary>Initial delay after pressing a button to begin echoing the input.</summary>
    [Export] public double EchoDelay = 0.3;

    /// <summary>Delay between moves while holding an input down.</summary>
    [Export] public double EchoInterval = 0.03;

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

    /// <summary>Move the cursor in a direction.</summary>
    /// <param name="direction">Direction to move. Multiplied by the grid size to get the new position.</param>
    public void Move(Vector2I direction) => EmitSignal(SignalName.DirectionPressed, Position + direction*LevelManager.GridSize + LevelManager.CellSize/2);

    /// <summary>Update the grid cell when the pointer signals it has moved.</summary>
    /// <param name="previous">Previous position of the pointer.</param>
    /// <param name="current">Next position of the pointer.</param>
    public void OnPointerMoved(Vector2 previous, Vector2 current) => Cell = LevelManager.CellOf(current);

    /// <summary>Start/continue echo movement of the cursor.</summary>
    public void OnEchoTimeout()
    {
        Move(_direction);
        if (EchoInterval > GetProcessDeltaTime())
        {
            EchoTimer.WaitTime = EchoInterval;
            EchoTimer.Start();
        }
        else
            _echoing = true;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (InputManager.Mode == InputMode.Digital)
        {
            Vector2I dir = InputManager.GetDigitalVector();
            if (dir != _direction)
            {
                EchoTimer.Stop();
                _echoing = false;

                if (dir != Vector2I.Zero)
                {
                    if (dir.Abs().X + dir.Abs().Y > _direction.Abs().X + _direction.Abs().Y)
                        Move(dir - _direction);
                    else
                        Move(dir);
                    _direction = dir;

                    EchoTimer.WaitTime = EchoDelay;
                    EchoTimer.Start();
                }
                else
                    _direction = Vector2I.Zero;
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (InputManager.Mode == InputMode.Digital && _echoing)
            Move(_direction);
    }
}