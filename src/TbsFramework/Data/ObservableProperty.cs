using System.Collections.Generic;

namespace TbsFramework.Data;

/// <summary>Property that raises an event whenever its value changes.</summary>
/// <typeparam name="T">Data type of the property.</typeparam>
/// <param name="initial">Initial value of the property.</param>
public class ObservableProperty<T>(T initial=default)
{
    /// <summary>Handler for changes in the property's value.</summary>
    /// <param name="old">Previous value before the change.</param>
    /// <param name="new">New value after the change.</param>
    public delegate void ValueChangedEventHandler(T old, T @new);

    public static implicit operator ObservableProperty<T>(T value) => new(value);
    public static implicit operator T(ObservableProperty<T> value) => value.Value;

    /// <summary>Signals that the property's value has changed.</summary>
    public event ValueChangedEventHandler ValueChanged;

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