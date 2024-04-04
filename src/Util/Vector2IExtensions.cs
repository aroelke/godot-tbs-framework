using Godot;

namespace Util;

/// <summary>Extensions for <c>Vector2I</c>.</summary>
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
}