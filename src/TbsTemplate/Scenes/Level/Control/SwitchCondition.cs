using Godot;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>
/// Represents a condition under which an AI-controlled unit's <see cref="Behavior"/> switches from one type
/// to another.
/// </summary>
[Icon("res://icons/SwitchCondition.svg"), Tool]
public abstract partial class SwitchCondition : Node
{
    /// <summary>Signals that a behavior switch has occurred.</summary>
    /// <param name="satisfied">Whether the switch occurred due to satisfying the condition or not.</param>
    [Signal] public delegate void BehaviorSwitchTriggeredEventHandler(bool satisfied);

    /// <summary>Signals that the behavior switch condition has become satisfied.</summary>
    [Signal] public delegate void SwitchConditionSatisfiedEventHandler();

    /// <summary>Signals that the behavior switch condition has become unsatisfied.</summary>
    [Signal] public delegate void SwitchConditionUnsatisfiedEventHandler();

    private bool _satisfied = false;

    /// <summary>Whether or not the condition is currently satisfied.</summary>
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

    /// <summary>Reset the condition back to being unsatisfied.</summary>
    /// <remarks><b>Note</b>: This is mainly meant for testing. It does not affect any other state of the condition.</remarks>
    public void Reset() => Satisfied = false;
}