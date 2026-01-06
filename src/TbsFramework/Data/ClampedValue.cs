using System;
using System.Collections.Generic;

namespace TbsFramework.Data;

/// <summary>Represents a value that is bounded to a minimum and a maximum.  Also has signals for watching value changes.</summary>
/// <typeparam name="T">Data type of the value.</typeparam>
public class ClampedValue<T> where T : IComparable<T>
{
    /// <summary>Handler for changes in the value.</summary>
    /// <param name="old">Value before the change.</param>
    /// <param name="new">Value after the change.</param>
    public delegate void ValueChangedEventHandler(T old, T @new);

    /// <summary>Handler for changes in the range. If the current value was outside the new range, it may have changed as well.</summary>
    /// <param name="oldMin">Minimum value before the range change.</param>
    /// <param name="newMin">Minimum value after the range change.</param>
    /// <param name="oldMax">Maximum value before the range change.</param>
    /// <param name="newMax">Maximum value after the range change.</param>
    /// <param name="oldValue">Current value before the range change.</param>
    /// <param name="newValue">Current value clamped to the new range.</param>
    public delegate void RangeChangedEventHandler(T oldMin, T newMin, T oldMax, T newMax, T oldValue, T newValue);

    /// <summary>Signals that the value has changed.</summary>
    public event ValueChangedEventHandler ValueChanged;
    /// <summary>Signals that the range has changed.</summary>
    public event RangeChangedEventHandler RangeChanged;

    private T _value, _min, _max;
    private static T Clamp(T value, T min, T max) => value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;

    /// <summary>
    /// Current value. If set to something outside the range, is automatically clamped to be within the range. Changes raise the <see cref="ValueChanged"/> event,
    /// but not if clamping it causes no change.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            T next = Clamp(value, _min, _max);
            if (!EqualityComparer<T>.Default.Equals(next, _value))
            {
                T old = _value;
                _value = next;
                if (ValueChanged is not null)
                    ValueChanged(old, _value);
            }
        }
    }

    /// <summary>
    /// Current minimum of the range. If set above <see cref="Maximum"/>, <see cref="Maximum"/> will be raised to match. After setting the new minimum,
    /// <see cref="Value"/> may also change to reflect the new range. Changes raise the <see cref="RangeChanged"/> event.
    /// </summary>
    public T Minimum
    {
        get => _min;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_min, value))
            {
                T oldMin = _min, oldMax = _max, oldVal = _value;
                _min = value;
                if (_min.CompareTo(_max) > 0)
                    _max = _min;
                _value = Clamp(_value, _min, _max);
                if (RangeChanged is not null)
                    RangeChanged(oldMin, _min, oldMax, _max, oldVal, _value);
                if (!EqualityComparer<T>.Default.Equals(_value, oldVal) && ValueChanged is not null)
                    ValueChanged(oldVal, _value);
            }
        }
    }

    /// <summary>
    /// Current maximum of the range. If set to below <see cref="Minimum"/>, <see cref="Minimum"/> will be lowered to match. After setting the new maximum,
    /// <see cref="Value"/> may also change to reflect the new range. Changes raise the <see cref="RangeChanged"/> event.
    /// </summary>
    public T Maximum
    {
        get => _max;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_max, value))
            {
                T oldMin = _min, oldMax = _max, oldVal = _value;
                _max = value;
                if (_max.CompareTo(_min) < 0)
                    _min = _max;
                _value = Clamp(_value, _min, _max);
                if (RangeChanged is not null)
                    RangeChanged(oldMin, _min, oldMax, _max, oldVal, _value);
                if (!EqualityComparer<T>.Default.Equals(_value, oldVal) && ValueChanged is not null)
                    ValueChanged(oldVal, _value);
            }
        }
    }

    public ClampedValue(T value, T min, T max)
    {
        if (min.CompareTo(max) > 0)
            throw new ArgumentException($"Minimum value {min} is higher than maximum value {max}");

        _min = min;
        _max = max;
        _value = Clamp(value, _min, _max);
    }

    public ClampedValue(T min, T max) : this(min, min, max) {}
}