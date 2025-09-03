using Godot;

namespace TbsTemplate.Scenes.Level.Control;

public abstract partial class SwitchCondition : Node
{
    [Signal] public delegate void BehaviorSwitchTriggeredEventHandler(bool satisfied);

    [Signal] public delegate void SwitchConditionSatisfiedEventHandler();
    
    [Signal] public delegate void SwitchConditionUnsatisfiedEventHandler();

    private bool _satisfied = false;

    public bool Satisfied
    {
        get => _satisfied;
        protected set
        {
            if (_satisfied != value)
            {
                EmitSignal(SignalName.BehaviorSwitchTriggered, _satisfied = value);
                if (_satisfied)
                    EmitSignal(SignalName.SwitchConditionSatisfied);
                else
                    EmitSignal(SignalName.SwitchConditionUnsatisfied);
            }
        }
    }

    public void Reset() => Satisfied = false;
}