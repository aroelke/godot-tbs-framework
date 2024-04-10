using Godot;

namespace Extensions;

/// <summary>Extensions for <see cref="Rect2"/>.</summary>
public static class Rect2Extensions
{
    /// <summary>Determine if one <see cref="Rect2"/> entirely encloses another.</summary>
    /// <param name="rect">Enclosing <see cref="Rect2"/>.</param>
    /// <param name="b"><see cref="Rect2"/> to test.</param>
    /// <param name="perimeter"><c>true</c> if shared edges should count as enclosing, <c>false</c> if they shouldn't.</param>
    /// <returns><c>true</c> if <paramref name="rect"/> completely encloses <paramref name="b"/>, and <c>false</c> otherwise.</returns>
    public static bool Contains(this Rect2 rect, Rect2 b, bool perimeter=false)
    {
        if (perimeter)
            return b.Position.X >= rect.Position.X && b.Position.Y >= rect.Position.Y && b.End.X <= rect.End.X && b.End.Y <= rect.End.Y;
        else
            return b.Position.X > rect.Position.X && b.Position.Y > rect.Position.Y && b.End.X < rect.End.X && b.End.Y < rect.End.Y;
    }

    /// <summary>Determine if a <see cref="Rect2"/> contains a point.</summary>
    /// <param name="rect">Enclosing <see cref="Rect2"/>.</param>
    /// <param name="point">Point to test.</param>
    /// <param name="perimeter"><c>true</c> if a point lying on the edge of the <see cref="Rect2"/> should count as contained,
    /// <c>false</c> if it shouldn't.</param>
    /// <returns><c>true</c> if the point lies within the <see cref="Rect2"/>, and <c>false</c> otherwise.</returns>
    public static bool Contains(this Rect2 rect, Vector2 point, bool perimeter=false)
    {
        if (perimeter)
            return point.X >= rect.Position.X && point.Y >= rect.Position.Y && point.X <= rect.End.X && point.Y <= rect.End.Y;
        else
            return point.X > rect.Position.X && point.Y > rect.Position.Y && point.X < rect.End.X && point.Y < rect.End.Y;
    }
}