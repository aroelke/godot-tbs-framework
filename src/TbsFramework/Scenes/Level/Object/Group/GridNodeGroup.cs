using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsFramework.Scenes.Level.Object.Group;

/// <summary>A  group of <see cref="GridNode"/>s to facilitate managing and iterating over them.</summary>
[Icon("res://icons/GridNodeGroup.svg")]
public partial class GridNodeGroup : Node, IEnumerable<GridNode>, IEnumerable
{
    /// <summary>Number of <see cref="GridNode"/>s in the group.</summary>
    public int Count => GetChildren().Count(static (c) => c is GridNode);

    /// <param name="item">Item to look for.</param>
    /// <returns><c>true</c> if the grid node group contains <paramref name="item"/>, and <c>false</c> otherwise.</returns>
    public bool Contains(GridNode item) => GetChildren().Contains(item);

    public IEnumerator<GridNode> GetEnumerator() => GetChildren().OfType<GridNode>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetChildren().OfType<GridNode>().GetEnumerator();
}