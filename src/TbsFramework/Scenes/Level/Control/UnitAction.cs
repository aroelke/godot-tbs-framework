using System.Collections.Generic;
using Godot;

namespace TbsFramework.Scenes.Level.Control;

/// <summary>Informationa about a unit's potential action.</summary>
/// <param name="Name">Name of the action.</param>
/// <param name="Source">Cells the action could be performed from.</param>
/// <param name="Target">Cell the action will be performed on.</param>
/// <param name="Traversable">Cells the acting unit can move on.</param>
public record class UnitAction(StringName Name, IEnumerable<Vector2I> Source, Vector2I Target, IEnumerable<Vector2I> Traversable)
{
    public static readonly StringName AttackAction = "Attack";
    public static readonly StringName SupportAction = "Support";
    public static readonly StringName EndAction = "End";
}