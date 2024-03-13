using Godot;

namespace Util;

public static class Rect2Extensions
{
    public static bool Contains(this Rect2 rect, Rect2 b) => b.Position.X > rect.Position.X && b.Position.Y > rect.Position.Y && b.End.X < rect.End.X && b.End.Y < rect.End.Y;

    public static bool Contains(this Rect2 rect, Vector2 point) => point.X > rect.Position.X && point.Y > rect.Position.Y && point.X < rect.End.X && point.Y < rect.End.Y;
}