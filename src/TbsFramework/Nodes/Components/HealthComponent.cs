using System;
using Godot;

namespace TbsFramework.Nodes.Components;

/// <summary>Node component maintaining health. Signals when health changes.</summary>
[Tool]
public partial class HealthComponent : Node
{
    /// <summary>Indicates that the current health value has changed.</summary>
    /// <param name="value">New health value.</param>
    [Signal] public delegate void ValueChangedEventHandler(double value);

    /// <summary>Indicates that the maximum health value has changed.</summary>
    /// <param name="value">New maximum health value.</param>
    /// <remarks>If the maximum is set below the current, this fill fire before <see cref="ValueChanged"/></remarks>
    [Signal] public delegate void MaximumChangedEventHandler(double value);

    private double _max = 0, _current = 0;

    /// <summary>Max health value. Is always nonnegative. If set to a value below <see cref="Value"/>, also changes <see cref="Value"/>.</summary>
    [Export] public double Maximum
    {
        get => _max;
        set
        {
            double next = Math.Max(value, 0);
            if (_max != next)
            {
                _max = next;
                EmitSignal(SignalName.MaximumChanged, _max);
                if (Value > _max)
                    Value = _max;
            }
        }
    }

    /// <summary>Current health value. Is always between 0 and <see cref="Maximum"/>, inclusive.</summary>
    [Export] public double Value
    {
        get => _current;
        set
        {
            double next = Mathf.Clamp(value, 0, Maximum);
            if (_current != next)
            {
                _current = next;
                EmitSignal(SignalName.ValueChanged, _current);
            }
        }
    }
}