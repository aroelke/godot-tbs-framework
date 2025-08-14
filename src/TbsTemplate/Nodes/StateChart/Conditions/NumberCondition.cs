using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary><see cref="Chart"/> action condition that evaluates a single numerical property and compares it to a static value.</summary>
/// <typeparam name="T">Numerical type</typeparam>
[Icon("res://icons/statechart/NumberCondition.svg"), Tool]
public abstract partial class NumberCondition<[MustBeVariant] T> : Condition where T : INumber<T>
{
    private const string Equal          = "=";
    private const string NotEqual       = "\u2260";
    private const string Less           = "<";
    private const string LessOrEqual    = "\u2264";
    private const string Greater        = ">";
    private const string GreaterOrEqual = "\u2265";

    private static readonly Dictionary<string, Func<T, T, bool>> Comparisons = new()
    {
        { Equal,          (a, b) => a == b },
        { NotEqual,       (a, b) => a != b },
        { Less,           (a, b) => a < b  },
        { LessOrEqual,    (a, b) => a <= b },
        { Greater,        (a, b) => a > b  },
        { GreaterOrEqual, (a, b) => a >= b }
    };

    /// <summary>Function to use to compare the constant to the property.</summary>
    [Export(PropertyHint.Enum, $"{Equal},{NotEqual},{Less},{LessOrEqual},{Greater},{GreaterOrEqual}")] public string Comparison = Equal;

    /// <summary>Name of the property to use for comparison.</summary>
    [Export] public StringName Number = "";

    /// <summary>Value to compare with the property. Note that this is the first operand used in <see cref="Comparison"/></summary>
    public abstract T Value { get; set; }

    public override bool IsSatisfied(ChartNode source) => Comparisons[Comparison](Value, source.StateChart.GetExpressionProperty(Number).As<T>());
}