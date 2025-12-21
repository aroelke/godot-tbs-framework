using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary><see cref="Map.Grid"/> debugging tool that encloses cell occupants in a rectangle. Turn invisible to turn off.</summary>
public partial class OccupantHighlighter : Node2D
{
    private Grid _grid = null;
    private Grid Grid => _grid ??= GetParentOrNull<Grid>();

    /// <summary>Highlight color to use for cells not associated with a color (such as <see cref="Unit"/>s).</summary>
    [Export] public Color DefaultColor = Colors.Black;

    public override void _Ready()
    {
        base._Ready();
        Position = Grid.Position;
    }

    public override void _Draw()
    {
        base._Draw();

        foreach ((Vector2I cell, GridNode node) in Grid.Occupants)
            DrawRect(Grid.CellRect(cell), node is Unit unit ? unit.Army.Faction.Color : DefaultColor, filled:false);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Visible)
            QueueRedraw();
    }
}