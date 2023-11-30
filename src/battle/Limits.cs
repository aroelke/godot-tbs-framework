using Godot;

namespace battle;

/// <summary>Simple node used for drawing map extents in the editor to make map creation easier.</summary>
[Tool]
public partial class Limits : Node2D
{
    private BattleMap _map = null;
    private BattleMap Map { get => GetParent<BattleMap>(); }

    /// <summary>Color to draw the grid bounds in the editor.</summary>
    [Export] public Color GridColor = Colors.Black;

    public override void _Ready()
    {
        if (!Engine.IsEditorHint())
            QueueFree();
    }

    public override void _Draw()
    {
        base._Draw();
        if (Engine.IsEditorHint())
            DrawRect(new Rect2I(Vector2I.Zero, Map.Size*Map.CellSize), GridColor, filled:false);
    }
}