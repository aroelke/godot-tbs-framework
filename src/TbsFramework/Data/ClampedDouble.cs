using Godot;

namespace TbsFramework.Data;

public class ClampedDouble(double? value=null, double min=0, double max=double.PositiveInfinity)
{
    public delegate void ValueChangedEventHandler(double old, double @new);
    public delegate void RangeChangedEventHandler(double oldMin, double newMin, double oldMax, double newMax, double oldValue, double newValue);

    public event ValueChangedEventHandler ValueChanged;
    public event RangeChangedEventHandler RangeChanged;

    private double _value = value ?? min;
    private double _min = min;
    private double _max = max;

    public double Value
    {
        get => _value;
        set
        {
            double next = Mathf.Clamp(value, _min, _max);
            if (next != _value)
            {
                double old = _value;
                _value = next;
                if (ValueChanged is not null)
                    ValueChanged(old, _value);
            }
        }
    }

    public double Minimum
    {
        get => _min;
        set
        {
            if (_min != value)
            {
                double oldMin = _min, oldMax = _max, oldVal = _value;
                _min = value;
                if (_min > _max)
                    _max = _min;
                _value = Mathf.Clamp(_value, _min, _max);
                if (RangeChanged is not null)
                    RangeChanged(oldMin, _min, oldMax, _max, oldVal, _value);
                if (_value != oldVal && ValueChanged is not null)
                    ValueChanged(oldVal, _value);
            }
        }
    }

    public double Maximum
    {
        get => _max;
        set
        {
            if (_max != value)
            {
                double oldMin = _min, oldMax = _max, oldVal = _value;
                _max = value;
                if (_max < _min)
                    _min = _max;
                _value = Mathf.Clamp(_value, _min, _max);
                if (RangeChanged is not null)
                    RangeChanged(oldMin, _min, oldMax, _max, oldVal, _value);
                if (_value != oldVal && ValueChanged is not null)
                    ValueChanged(oldVal, _value);
            }
        }
    }
}