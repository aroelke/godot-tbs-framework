using Godot;
using level.manager;
using ui.input;

namespace level.ui;

/// <summary>Cursor on the grid used for highlighting a cell and selecting things in it.</summary>
public partial class Cursor : Sprite2D, ILevelManaged
{
    [Signal] public delegate void DirectionPressedEventHandler(Vector2 position);

    private InputManager _inputManager = null;
    private LevelManager _levelManager = null;
    private Vector2I _cell = Vector2I.Zero;

    private InputManager InputManager => _inputManager ??= GetNode<InputManager>("/root/InputManager");

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

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (InputManager.Mode == InputMode.Digital)
        {
            Vector2I dir = (Vector2I)Input.GetVector("cursor_digital_left", "cursor_digital_right", "cursor_digital_up", "cursor_digital_down").Round();
            Vector2 jump = Position + dir*LevelManager.GridSize + LevelManager.GridSize/2;
            EmitSignal(SignalName.DirectionPressed, jump);
        }
    }
}