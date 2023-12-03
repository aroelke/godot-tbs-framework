using Godot;

namespace battle;

/// <summary>Represents a piece of terrain on the battlefield, which can hinder movement modify stats.</summary>
[GlobalClass, Tool]
public partial class Terrain : Resource
{
    /// <summary>Cost to move onto this terrain.</summary>
    [Export] public int Cost = 1;
}
