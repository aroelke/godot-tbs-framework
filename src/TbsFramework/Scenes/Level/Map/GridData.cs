using System.Collections.Generic;
using Godot;

namespace TbsFramework.Scenes.Level.Map;

[GlobalClass, Tool]
public partial class GridData : Resource
{
    public Vector2I Size = Vector2I.One;

    public Dictionary<Vector2I, Terrain> Terrain;
}