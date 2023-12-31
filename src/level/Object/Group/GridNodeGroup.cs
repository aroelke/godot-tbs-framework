using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace level.Object.Group;

/// <summary>An abstract group of grid nodes of the same type to facilitate managing and iterating over them.</summary>
/// <typeparam name="T">Type of <c>GridNode</c> contained in the group.</typeparam>
public abstract partial class GridNodeGroup<[MustBeVariant] T> : Node, IEnumerable<T>, IEnumerable where T : GridNode
{
    /// <summary>Number of nodes in the group of type <c>T</c>.</summary>
    public int Count => GetChildren().Where((c) => c is T).Count();

    /// <param name="item">Item to look for.</param>
    /// <returns><c>true</c> if the grid node group contains the item, and <c>false</c> otherwise.</returns>
    public bool Contains(T item) => GetChildren().Contains(item);

    public IEnumerator<T> GetEnumerator() => GetChildren().OfType<T>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetChildren().OfType<T>().GetEnumerator();
}