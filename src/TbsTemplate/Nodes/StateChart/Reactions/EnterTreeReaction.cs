using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Allows a <see cref="State"/> to react to entering the scene tree.</summary>
public partial class EnterTreeReaction : Reaction, IReaction
{
    /// <summary>Signals that the <see cref="State"/> has entered the scene tree while active.</summary>
    [Signal] public delegate void StateEnteredTreeEventHandler();

    public void React() => EmitSignal(SignalName.StateEnteredTree);

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Active)
            React();
    }
}