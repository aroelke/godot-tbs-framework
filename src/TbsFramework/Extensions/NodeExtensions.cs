using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsFramework.Extensions;

/// <summary>Extension methods for <see cref="Node"/> and subclasses of <see cref="Node"/>.</summary>
public static class NodeExtensions
{
    /// <returns>All nodes in the scene tree that are in the subtree that has <paramref name="node"/> at the top, not including <paramref name="node"/>.</returns>
    public static IEnumerable<Node> GetDescendants<T>(this T node) where T : Node => node?.GetChildren().SelectMany((n) => (IEnumerable<Node>)[n, .. GetDescendants(n)]) ?? [];
}