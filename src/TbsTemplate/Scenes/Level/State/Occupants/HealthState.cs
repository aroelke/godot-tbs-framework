using Godot;

namespace TbsTemplate.Scenes.Level.State.Components;

[GlobalClass, Tool]
public partial class HealthState : Resource
{
    /// <summary>Indicates that the current health value has changed.</summary>
    /// <param name="from">Health value before the change.</param>
    /// <param name="to">New health value.</param>
    [Signal] public delegate void ValueChangedEventHandler(float from, float to);

    /// <summary>Indicates that the maximum health value has changed.</summary>
    /// <param name="from">Maximum health value before the change.</param>
    /// <param name="to">New maximum health value.</param>
    /// <remarks>If the maximum is set below the current, this will fire before <see cref="ValueChanged"/></remarks>
    [Signal] public delegate void MaximumChangedEventHandler(float from, float to);

    private float _max = 10, _value = 10;

    [Export] public float Value
    {
        get => _value;
        set
        {
            float prev = _value;
            float next = Mathf.Clamp(value, 0, _max);
            if (_value != next)
            {
                _value = next;
                EmitSignal(SignalName.ValueChanged, prev, next);
            }
        }
    }

    [Export] public float Maximum
    {
        get => _max;
        set
        {
            float prev = _max;
            float next = Mathf.Max(0, value);
            if (_max != next)
            {
                _max = next;
                EmitSignal(SignalName.MaximumChanged, prev, next);
                Value = Mathf.Clamp(_value, 0, _max);
            }
        }
    }
}