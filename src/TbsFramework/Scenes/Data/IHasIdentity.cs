using System.Collections.Generic;

namespace TbsFramework.Scenes.Data;

/// <summary>
/// Object that can be used for comparing two <see cref="IHasIdentity{T, U}"/>s.
/// </summary>
public static class HasIdentity
{
    /// <returns>
    /// <c>true</c> if <paramref name="a"/> and <paramref name="b"/> have the same <see cref="IHasIdentity{T, U}.Identity"/> and
    /// <c>false</c> otherwise.
    /// </returns>
    public static bool Equivalent<T, U>(IHasIdentity<T, U> a, IHasIdentity<T, U> b) => EqualityComparer<T>.Default.Equals(a.Identity, b.Identity);
}

/// <summary>
/// Interface for an object that could have multiple copies that all represent the same thing (for example, two duplicate <see cref="UnitData"/>s
/// represent the same unit). Not needed for data with only static members that doesn't need to be duplicated. Must be used for any object that
/// could be copied as a result of copying the grid, for example when the AI simulates action results.
/// </summary>
/// <typeparam name="T">Data type used for forming the reference.</typeparam>
/// <typeparam name="U">Type of object that has this type of identity.</typeparam>
public interface IHasIdentity<T, U>
{
    /// <summary>Object reference. The value should be preserved across copies.</summary>
    public T Identity { get; }

    /// <summary>Create a copy of this object, maintaining the same <see cref="Identity"/> value.</summary>
    public U Clone();
}