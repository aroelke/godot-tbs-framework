using System.Collections.Generic;
using Godot;
using Godot.Collections;
using TbsFramework.Nodes;
using TbsFramework.Scenes.Level.Map;

namespace TbsFramework.Scenes.Level.Object;

/// <summary>A node representing an object that moves on a <see cref="Map.Grid"/>.</summary>
[Icon("res://icons/GridNode.svg"), Tool]
public abstract partial class GridNode : BoundedNode2D
{
    /// <summary>Grid on which the containing object sits.</summary>
    [Export] public Grid Grid;

    /// <summary>Cell on the <see cref="Map.Grid"/> that this object currently occupies.</summary>
    [Export] public virtual Vector2I Cell
    {
        get => Data.Cell;
        set => Data.Cell = value;
    }

    public abstract GridObjectData Data { get; }

    /// <inheritdoc cref="BoundedNode2D.Size"/>
    /// <remarks>Grid nodes have a constant size that is based on the size of the <see cref="Map.Grid"/> cells.</remarks>
    public override Vector2 Size { get => Grid?.CellSize ?? Vector2.Zero; set {}}

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].As<StringName>() == PropertyName.Size)
            property["usage"] = property["usage"].As<uint>() | (uint)PropertyUsageFlags.ReadOnly;
        base._ValidateProperty(property);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (Grid is null)
            warnings.Add("No grid to move on has been defined.");
        else if (Cell.X < 0 || Cell.Y < 0 || Cell.X >= Grid.Size.X || Cell.Y >= Grid.Size.Y)
            warnings.Add("Outside grid bounds.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        Data.CellChanged += (cell) => {
            if (Grid is not null)
                GlobalPosition = Grid.PositionOf(cell) + Grid.GlobalPosition;
            if (Engine.IsEditorHint())
                UpdateConfigurationWarnings();
        };
    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint() && Grid is not null && !Input.IsMouseButtonPressed(MouseButton.Left))
        {
            Cell = Grid.CellOf(GlobalPosition - Grid.GlobalPosition + Size/2);
            GlobalPosition = Grid.PositionOf(Data.Cell) + Grid.GlobalPosition;
        }
    }
}