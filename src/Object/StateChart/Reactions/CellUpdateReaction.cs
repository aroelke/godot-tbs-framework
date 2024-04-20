using Godot;

namespace  Object.StateChart.Reactions;

public partial class CellUpdateReaction : Reaction
{
    [Signal] public delegate void StateCellUpdatedEventHandler(Vector2I cell);

    public void OnCellUpdated(Vector2I cell)
    {
        if (Active)
            EmitSignal(SignalName.StateCellUpdated, cell);
    }
}