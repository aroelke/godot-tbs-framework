using Godot;

namespace TbsTemplate.Extensions;

/// <summary>Extensions for <see cref="Vector2I"/>.</summary>
public static class Vector2IExtensions
{
    // List of the four cardinal directions
    public static readonly Vector2I[] Directions = [Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left];

    /// <summary>Determine if two cell coordinate pairs are adjacent.</summary>
    /// <param name="a">First pair for comparison.</param>
    /// <param name="b">Second pair for comparison.</param>
    /// <returns><c>true</c> if the two coordinate pairs are adjacent, and <c>false</c> otherwise.</returns>
    public static bool IsAdjacent(this Vector2I a, Vector2I b)
    {
        foreach (Vector2I direction in Directions)
            if (b - a == direction || a - b == direction)
                return true;
        return false;
    }

    /// <summary>Normalize a <see cref="Vector2I"/> into a unit vector (which isn't necessarily of integer type!).</summary>
    public static Vector2 Normalized(this Vector2I a) => ((Vector2)a).Normalized();

    /// <summary>Compute the dot product of two <see cref="Vector2I"/>s.</summary>
    /// <returns><paramref name="a"/> dot <paramref name="b"/>, or <c>a.X*b.X + a.Y*b.Y</c>.</returns>
    public static int Dot(this Vector2I a, Vector2I b) => (a*b).Sum();

    /// <returns>The sum of the two coordinates of the <see cref="Vector2I"/>.</returns>
    public static int Sum(this Vector2I a) => a.X + a.Y;

    /// <returns>
    /// The Manhattan distance (sum of the absolute values of the differences in coordinates) between <paramref name="a"/> and
    /// <paramref name="b"/>.
    /// </returns>
    public static int DistanceTo(this Vector2I a, Vector2I b) => (b - a).Abs().Sum();

    /// <summary>Find the parallel and perpendicular projections of a <see cref="Vector2I"/> onto another.</summary>
    /// <returns>
    /// A new <see cref="Vector2"/> whose X component is the length of the vector that's parallel to <paramref name="b"/>
    /// and whose Y component is the length of the vector that's perpendicular to <paramref name="b"/>.</returns>
    public static Vector2 ProjectionsTo(this Vector2I a, Vector2I b)
    {
        float parallel = ((Vector2)a).Dot(b.Normalized());
        return new(parallel, (a - parallel*b.Normalized()).Length());
    }
}