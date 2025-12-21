using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Layers;

/// <summary>Represents a set of cells in which a set of units can perform a special action.</summary>
public interface ISpecialActionRegion
{
    /// <summary>Name of the action to display for selection.</summary>
    public StringName Action { get; }

    /// <summary>Cells in which the special action can be performed.</summary>
    public ISet<Vector2I> Cells { get; }

    /// <returns><c>true</c> if <paramref name="unit"/> is allowed to perform the action (regardless of its position), and <c>false</c> otherwise.</returns>
    public bool IsAllowed(IUnit unit);

    /// <returns><c>true</c> if <paramref name="unit"/> can perform the action in <paramref name="cell"/>, and <c>false</c> otherwise.</returns>
    public bool CanPerform(IUnit unit, Vector2I cell) => IsAllowed(unit) && Cells.Contains(cell);
}