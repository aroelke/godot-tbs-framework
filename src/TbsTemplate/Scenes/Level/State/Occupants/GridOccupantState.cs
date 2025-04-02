using Godot;

namespace TbsTemplate.Scenes.Level.State.Occupants;

[GlobalClass, Tool]
public partial class GridOccupantState : Resource
{
    [Signal] public delegate void CellChangedEventHandler(Vector2I from, Vector2I to);

    private Vector2I _cell = Vector2I.Zero;

    [Export] public GridState Grid = null;

    [Export] public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I prev = _cell;
            Vector2I next = Grid?.Clamp(value) ?? value;
            if (_cell != next)
            {
                _cell = next;
                Grid?.SetOccupant(_cell, this);
                EmitSignal(SignalName.CellChanged, prev, next);
            }
        }
    }
}