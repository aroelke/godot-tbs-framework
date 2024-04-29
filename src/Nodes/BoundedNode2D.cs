using Godot;

namespace Object;

/// <summary>
/// A <see cref="Node2D"/> with additional size information that forms a <see cref="BoundingBox"/> along with its
/// <see cref="Node2D.Position"/>.
/// </summary>
[Icon("res://icons/BoundedNode2D.svg"), GlobalClass, Tool]
public partial class BoundedNode2D : Node2D
{
    /// <summary>Size of the node that forms its bounding box along with its <see cref="Node2D.Position"/>.</summary>
    [Export] public virtual Vector2 Size { get; set; }

    /// <summary>
    /// The bounding box of the node, composed of its <see cref="Node2D.Position"/> and <see cref="Size"/>. Setting
    /// this value will change both of those components.
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

    /// <summary>
    /// The global bounding box of the node, composed of its <see cref="Node2D.GlobalPosition"/> and <see cref="Size"/>.
    /// Setting this value will change both of those components.
    /// </summary>
    public Rect2 GlobalBoundingBox
    {
        get => new(GlobalPosition, Size);
        set
        {
            if (GlobalPosition != value.Position)
                GlobalPosition = value.Position;
            if (Size != value.Size)
                Size = value.Size;
        }
    }
}