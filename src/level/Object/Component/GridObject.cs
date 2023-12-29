using System;
using System.Collections.Generic;
using Godot;
using level.manager;

namespace level.Object.Component;

/// <summary>
/// Component of an object on a grid that maintains its grid location. That object's position is not automatically updated when
/// the grid cell changes; rather, it should listen for <c>CellChanged</c> and update it there.
/// </summary>
[Tool]
public partial class GridObject : Node
{
    /// <summary>Signals that the cell containing the object has changed.</summary>
    /// <param name="cell">New cell containing the object.</param>
    [Signal] public delegate void CellChangedEventHandler(Vector2I cell);

    private LevelManager _manager = null;
    private Vector2I _cell = Vector2I.Zero;

    /// <summary>Travel up the node tree until a grid manager is found, or return <c>null</c> if one isn't found.</summary>
    private LevelManager FindManager()
    {
        Node current = this;
        while (current is not null)
        {
            current = current.GetParent();
            if (current is null)
                return null;
            if (current is LevelManager manager)
                return manager;
        }
        return null;
    }

    /// <summary>Manager providing grid information.</summary>
    public LevelManager Manager => _manager ??= FindManager();

    /// <summary>Cell on the grid that this object currently occupies.</summary>
    [Export] public Vector2I Cell
    {
        get => _cell;
        set
        {
            Vector2I next = Manager?.Clamp(value) ?? value;
            if (next != _cell)
            {
                _cell = next;
                EmitSignal(SignalName.CellChanged, _cell);
            }
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        if (FindManager() == null)
            warnings.Add("The GridObject component has no manager");

        return warnings.ToArray();
    }
}