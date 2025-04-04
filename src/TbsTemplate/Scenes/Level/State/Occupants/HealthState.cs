using Godot;

namespace TbsTemplate.Scenes.Level.State.Components;

[GlobalClass, Tool]
public partial class HealthState : Resource
{
    /// <summary>Indicates that the current health value has changed.</summary>
    /// <param name="value">New health value.</param>
    [Signal] public delegate void ValueChangedEventHandler(double value);

    /// <summary>Indicates that the maximum health value has changed.</summary>
    /// <param name="value">New maximum health value.</param>
    /// <remarks>If the maximum is set below the current, this will fire before <see cref="ValueChanged"/></remarks>
    [Signal] public delegate void MaximumChangedEventHandler(double value);

    private double _max = 10, _value = 10;

    [Export] public double Value
    {
        get => _value;
        set
        {
            double next = Mathf.Clamp(value, 0, _max);
            if (_value != next)
            {
                _value = next;
                EmitSignal(SignalName.ValueChanged, next);
            }
        }
    }

    [Export] public double Maximum
    {
        get => _max;
        set
        {
            double next = Mathf.Max(0, value);
            if (_max != next)
            {
                _max = next;
                EmitSignal(SignalName.MaximumChanged, next);
                Value = Mathf.Clamp(_value, 0, _max);
            }
        }
    }
}