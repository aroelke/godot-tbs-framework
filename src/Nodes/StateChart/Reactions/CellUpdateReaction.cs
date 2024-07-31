using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>
/// Allows a <see cref="States.State"/> to react to events regarding individual <see cref="Level.Map.Grid"/> cells. The name of the reaction
/// node should be used to indicate what the update represents.
/// </summary>
public partial class CellUpdateReaction : Reaction
{
    /// <summary>Signals that an update has been made to a cell.</summary>
    /// <param name="cell">Cell that was updated.</param>
    [Signal] public delegate void StateCellUpdatedEventHandler(Vector2I cell);

    public void OnCellUpdated(Vector2I cell)
    {
        if (Active)
            EmitSignal(SignalName.StateCellUpdated, cell);
    }
}