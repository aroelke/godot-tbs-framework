using System.Collections.Generic;

namespace TbsFramework.Data;

/// <summary>Handler for changes in a property's value.</summary>
/// <param name="from">Previous value before the change.</param>
/// <param name="to">New value after the change.</param>
public delegate void PropertyChangedEventHandler<in T>(T from, T to);

/// <summary>Property that raises an event whenever its value changes.</summary>
/// <typeparam name="T">Data type of the property.</typeparam>
/// <param name="initial">Initial value of the property.</param>
public class ObservableProperty<T>(T initial=default)
{
    public static implicit operator ObservableProperty<T>(T value) => new(value);
    public static implicit operator T(ObservableProperty<T> value) => value.Value;

    /// <summary>Signals that the property's value has changed.</summary>
    public event PropertyChangedEventHandler<T> ValueChanged;

    private T _value = initial;

    /// <summary>Current value of the property.</summary>
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                T old = _value;
                _value = value;
                if (ValueChanged is not null)
                    ValueChanged(old, _value);
            }
        }
    }
}