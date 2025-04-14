using Godot;

namespace TbsTemplate.Scenes.Level.Map;

/// <summary>Defines necessary functionality for maintaining the game state and performing computations based on it.</summary>
public interface IGrid
{
    /// <summary>Grid dimensions. Both elements should be positive.</summary>
    public Vector2I Size { get; }
}