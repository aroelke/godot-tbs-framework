using Godot;

namespace Object;

/// <summary>A <c>Node2D</c> with additional size information that forms a bounding box along with its position.</summary>
[Icon("res://icons/BoundedNode2D.svg"), GlobalClass, Tool]
public partial class BoundedNode2D : Node2D
{
    /// <summary>Size of the node that forms its bounding box along with its <c>Position</c>.</summary>
    [Export] public virtual Vector2 Size { get; set; }

    /// <summary>
    /// The bounding box of the node, composed of its <c>Position</c> and <c>Size</c>. Setting this value with change both
    /// of those components.
    /// </summary>
    public Rect2 BoundingBox
    {
        get => new(Position, Size);
        set
        {
            if (Position != value.Position)
                Position = value.Position;
            if (Size != value.Size)
                Size = value.Size;
        }
    }
}