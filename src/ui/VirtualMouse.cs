using battle;
using Godot;

namespace ui;

/// <summary>Virtual cursor that can be moved via mouse movement, digitally, or via analog.</summary>
public partial class VirtualMouse : Sprite2D
{
    [Signal] public delegate void MovedEventHandler(Vector2 previous, Vector2 current);

    /// <summary>
    /// Method used for moving the virtual mouse cursor: the real mouse, using digital input (e.g. keyboard keys, controller dpad), or
    /// using analog inputs (e.g. controller analog sticks).
    /// </summary>
    public enum Mode { Mouse, Digital, Analog }

    private bool _tracking = false;
    private Vector2 _previous = Vector2.Zero;
    private BattleMap _map = null;
    private Vector2I _mask = Vector2I.Zero;

    private BattleMap Map => _map ??= GetParent<BattleMap>();

    private Vector2 MousePosition() => GetParent<CanvasItem>()?.GetLocalMousePosition() ?? GetGlobalMousePosition();

    /// <summary>Current mode used for moving the virtual mouse.</summary>
    public Mode MoveMode { get; private set; } = Mode.Mouse;

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case NotificationWMMouseEnter: case NotificationVpMouseEnter:
                Position = MousePosition();
                _tracking = true;
                break;
            case NotificationWMMouseExit: case NotificationVpMouseExit:
                _tracking = false;
                break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseMotion)
        {
            if (_tracking)
            {
                Position = MousePosition();
                MoveMode = Mode.Mouse;
            }
        }
        else
        {
            Vector2I? target = null;
            Vector2I current = Map.CellOf(Position);

            Vector2I skip = (Vector2I)Input.GetVector("cursor_skip_left", "cursor_skip_right", "cursor_skip_up", "cursor_skip_down").Round();
            if (skip != Vector2.Zero)
            {
                target = new(
                    skip.X < 0 ? 0 : skip.X > 0 ? Map.Size.X - 1 : current.X,
                    skip.Y < 0 ? 0 : skip.Y > 0 ? Map.Size.Y - 1 : current.Y
                );
            }
            else
            {
                Vector2I move = (Vector2I)Input.GetVector("cursor_left", "cursor_right", "cursor_up", "cursor_down").Round();
                if (!@event.IsEcho() && move != _mask)
                    move = new Vector2I(_mask.X == 0 ? move.X : 0, _mask.Y == 0 ? move.Y : 0);
                if (!@event.IsEcho())
                {
                    if (@event.IsPressed())
                        _mask = new(_mask.X | move.Abs().X, _mask.Y | move.Abs().Y);
                    else if (@event.IsReleased())
                        _mask = new(_mask.X & move.Abs().X, _mask.Y & move.Abs().Y);
                    else
                        _mask = Vector2I.Zero;
                }
                target = move + current;
            }

            if (target != null)
            {
                Position = Map.PositionOf(Map.Clamp(target.Value)) + Map.CellSize/2;
                MoveMode = Mode.Digital;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (MoveMode == Mode.Mouse && _tracking && Position != MousePosition())
            Position = MousePosition();

        if (Position != _previous)
        {
            EmitSignal(SignalName.Moved, _previous, Position);
            _previous = Position;
        }
    }
}