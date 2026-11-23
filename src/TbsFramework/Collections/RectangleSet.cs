using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsFramework.Collections;

/// <summary>Set representing all the points in a <see cref="Rect2I"/>.</summary>
/// <param name="bounds">Rectangle defining the points in the set.</param>
public class RectangleSet(Rect2I bounds) : IEnumerable<Vector2I>, IReadOnlySet<Vector2I>
{
    public int Count => bounds.Size.X*bounds.Size.Y;

    public bool Contains(Vector2I value) => value.X >= bounds.Position.X && value.X < bounds.Position.X + bounds.Size.X && value.Y >= bounds.Position.Y && value.Y < bounds.Position.Y + bounds.Size.Y;

    public bool IsProperSubsetOf(IEnumerable<Vector2I> other)
    {
        HashSet<Vector2I> distinct = [.. other];
        return Count < distinct.Count && this.All(distinct.Contains);
    }

    public bool IsProperSupersetOf(IEnumerable<Vector2I> other)
    {
        HashSet<Vector2I> distinct = [.. other];
        return Count > distinct.Count && distinct.All(Contains);
    }

    public bool IsSubsetOf(IEnumerable<Vector2I> other) => this.All(other.Contains);
    public bool IsSupersetOf(IEnumerable<Vector2I> other) => other.All(Contains);
    public bool Overlaps(IEnumerable<Vector2I> other) => other.Any(Contains);
    public bool SetEquals(IEnumerable<Vector2I> other) => IsSubsetOf(other) && IsSupersetOf(other);

    public IEnumerator<Vector2I> GetEnumerator() => Enumerable.Range(bounds.Position.Y, bounds.Size.Y)
        .SelectMany((r) => Enumerable.Range(bounds.Position.X, bounds.Size.X).Select((c) => bounds.Position + new Vector2I(c, r)))
        .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}