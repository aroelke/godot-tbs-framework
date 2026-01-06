using System;
using System.Collections.Generic;

namespace TbsFramework.Data;

public class ClampedValue<T> where T : IComparable<T>
{
    public delegate void ValueChangedEventHandler(T old, T @new);
    public delegate void RangeChangedEventHandler(T oldMin, T newMin, T oldMax, T newMax, T oldValue, T newValue);

    public event ValueChangedEventHandler ValueChanged;
    public event RangeChangedEventHandler RangeChanged;

    private T _value, _min, _max;
    private static T Clamp(T value, T min, T max) => value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;

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
        _value = value;
        _min = min;
        _max = max;
    }

    public ClampedValue(T min, T max) : this(min, min, max) {}
}