using Godot;

namespace Extensions;

/// <summary>Extensions for <see cref="Vector2I"/>.</summary>
public static class Vector2IExtensions
{
    // List of the four cardinal directions
    public static readonly Vector2I[] Directions = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };

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

    /// <returns>The sum of the two coordinates of the <see cref="Vector2I"/>.</returns>
    public static int Sum(this Vector2I a) => a.X + a.Y;

    /// <returns>
    /// The Manhattan distance (sum of the absolute values of the differences in coordinates) between <paramref name="a"/> and
    /// <paramref name="b"/>.
    /// </returns>
    public static int DistanceTo(this Vector2I a, Vector2I b) => (b - a).Abs().Sum();

    /// <returns>A new <see cref="Vector2I"/> with the coordinates of <paramref name="a"/> switched.</returns>
    public static Vector2I Inverse(this Vector2I a) => new(a.Y, a.X);
}