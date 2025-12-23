using Godot;

namespace TbsFramework.Nodes.StateCharts.Reactions;

/// <summary>Allows a <see cref="State"/> to react to entering the scene tree.</summary>
public partial class EnterTreeReaction : StateReaction0
{
    /// <summary>Signals that the <see cref="State"/> has entered the scene tree while active.</summary>
    [Signal] public delegate void StateEnteredTreeEventHandler();

    /// <summary>Signals that this node has entered the scene tree in an active state.</summary>
    public EnterTreeReaction() : base(SignalName.StateEnteredTree) {}

    public override void _EnterTree()
    {
        base._EnterTree();
        React();
    }
}