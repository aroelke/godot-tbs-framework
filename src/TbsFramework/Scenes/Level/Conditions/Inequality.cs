using System;

namespace TbsFramework.Scenes.Level.Conditions;

/// <summary>Represents a mathematical inequality that compares to comparable values.</summary>
public enum Inequality
{
    /// <summary>Indicates that two values are equal.</summary>
    Equal,
    /// <summary>Indicates that two values are not equal.</summary>
    NotEqual,
    /// <summary>Indicates that the first value is greater than the second value.</summary>
    Greater,
    /// <summary>Indicates that the first value is greater than or equal to the second value.</summary>
    GreaterOrEqual,
    /// <summary>Indicates that the first value is less than the second value.</summary>
    Less,
    /// <summary>Indicates that the first value is less than or equal to the second value.</summary>
    LessOrEqual
}

public static class InequalityExtensions
{
    /// <summary>Perform the comparison operation represented by <paramref name="inequality"/>.</summary>
    /// <typeparam name="T">Type of data being compared.</typeparam>
    /// <param name="inequality">Comparison operation to perform.</param>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns><c>true</c> if <paramref name="a"/> and <paramref name="b"/> satisfy <paramref name="inequality"/>, or <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentException">If <paramref name="inequality"/> is not one of the six <see cref="Inequality"/> values.</exception>
    public static bool Compare<T>(this Inequality inequality, T a, T b) where T : IComparable<T> => inequality switch {
        Inequality.Equal   => a.CompareTo(b) == 0,
        Inequality.Greater => a.CompareTo(b) > 0,
        Inequality.Less    => a.CompareTo(b) < 0,
        Inequality.NotEqual       => !Inequality.Equal.Compare(a, b),
        Inequality.GreaterOrEqual => Inequality.Greater.Compare(a, b) || Inequality.Equal.Compare(a, b),
        Inequality.LessOrEqual    => Inequality.Less.Compare(a, b) || Inequality.Equal.Compare(a, b),
        _ => throw new ArgumentException($"Unknown inequality {inequality}")
    };
}