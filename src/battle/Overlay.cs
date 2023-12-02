using Godot;

namespace battle;

/// <summary>Map overlay tile set for displaying traversable and attackable cells.</summary>
public partial class Overlay : TileMap
{
	/// <summary>Draw the cells that can be traversed.</summary>
	/// <param name="cells">List of cells that can be traversed, in any order.</param>
	public void DrawOverlay(Vector2I[] cells)
	{
		Clear();
		foreach (Vector2I cell in cells)
			SetCell(0, cell, 0, new(0, 0));
	}
}