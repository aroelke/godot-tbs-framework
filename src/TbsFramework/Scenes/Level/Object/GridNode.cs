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
    private Grid _grid = null;

    /// <summary>Grid on which the containing object sits.</summary>
    [Export] public Grid Grid
    {
        get => _grid;
        set
        {
            if (_grid != value)
            {
                _grid = value;
                if (_grid is not null)
                    Cell = _grid.CellOf(GetGridPosition() + Size/2);
            }
        }
    }

    /// <summary>Cell on the <see cref="Map.Grid"/> that this object currently occupies.</summary>
    [Export] public virtual Vector2I Cell
    {
        get => Data.Cell;
        set => Data.Cell = value;
    }

    public abstract GridObjectData Data { get; }

    /// <inheritdoc cref="BoundedNode2D.Size"/>
    /// <remarks>Grid nodes have a constant size that is based on the size of the <see cref="Map.Grid"/> cells.</remarks>
    public override Vector2 Size { get => _grid?.CellSize ?? Vector2.Zero; set {}}

    public Vector2 GetGridPosition() => GlobalPosition - _grid.GlobalPosition;

    public void SetGridPosition(Vector2 position) => GlobalPosition = _grid.GlobalPosition + position;

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].As<StringName>() == PropertyName.Size)
            property["usage"] = property["usage"].As<uint>() | (uint)PropertyUsageFlags.ReadOnly;
        base._ValidateProperty(property);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (_grid is null)
            warnings.Add("No grid to move on has been defined.");
        else if (Cell.X < 0 || Cell.Y < 0 || Cell.X >= _grid.Size.X || Cell.Y >= _grid.Size.Y)
            warnings.Add("Outside grid bounds.");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            Data.CellChanged += (cell) => {
                if (_grid is not null)
                    SetGridPosition(_grid.PositionOf(cell));
                if (Engine.IsEditorHint())
                    UpdateConfigurationWarnings();
            };

            // Do this after all nodes have been initialized so the backing data is defined
            Callable.From(() => {
                // Set cell first because otherwise all the grid nodes will go to (1, 1) and trigger occupancy exceptions
                Data.Cell = _grid.CellOf(GetGridPosition() + Size/2);
                Data.Grid = _grid.Data;
            }).CallDeferred();
        }
    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint() && _grid is not null && !Input.IsMouseButtonPressed(MouseButton.Left))
        {
            Data.Cell = _grid.CellOf(GetGridPosition() + Size/2);
            SetGridPosition(_grid.PositionOf(Data.Cell));
        }
    }
}