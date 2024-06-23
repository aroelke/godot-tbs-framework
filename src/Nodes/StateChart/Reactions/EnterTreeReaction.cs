using Godot;
using Nodes.StateChart.States;

namespace Nodes.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to react to entering the scene tree.</summary>
public partial class EnterTreeReaction : Reaction
{
    /// <summary>Signals that the <see cref="State"/> has entered the scene tree while active.</summary>
    [Signal] public delegate void StateEnteredTreeEventHandler();

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Active)
            EmitSignal(SignalName.StateEnteredTree);
    }
}