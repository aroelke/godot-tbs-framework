using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Level.Object.Group;

/// <summary>A  group of grid nodes to facilitate managing and iterating over them.</summary>
public partial class GridNodeGroup : Node, IEnumerable<GridNode>, IEnumerable
{
    /// <summary>Number of nodes in the group of type <c>T</c>.</summary>
    public int Count => GetChildren().Where((c) => c is GridNode).Count();

    /// <param name="item">Item to look for.</param>
    /// <returns><c>true</c> if the grid node group contains the item, and <c>false</c> otherwise.</returns>
    public bool Contains(GridNode item) => GetChildren().Contains(item);

    public IEnumerator<GridNode> GetEnumerator() => GetChildren().OfType<GridNode>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetChildren().OfType<GridNode>().GetEnumerator();
}