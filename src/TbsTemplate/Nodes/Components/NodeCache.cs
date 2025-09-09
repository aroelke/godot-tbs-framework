using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Nodes.Components;

/// <summary>
/// Node component (that's instantiated directly in code, not in in the scene editor) that cachees child references.
/// Does not cache <c>null</c> results.
/// </summary>
/// <param name="source">Node to cache references for.</param>
public class NodeCache(Node source)
{
    private readonly Dictionary<NodePath, Node> _cache = [];

    /// <summary>
    /// Fetches a node. The <see cref="NodePath"/> can be either a relative path (from the source node) or an absolute
    /// path (in the scene tree) to a node. If the path does not exist, a null instance is returned, an error is logged,
    /// and the result is not cached.
    /// </summary>
    /// <param name="path">Path to the node to get relative to <see cref="source"/>.</param>
    /// <returns>The <see cref="Node"/> at the given path.</returns>
    /// <remarks>
    /// Note: Fetching absolute paths only works when the node is inside the scene tree
    /// (see <see cref="Node.IsInsideTree"/>).
    /// </remarks>
    public Node GetNode(NodePath path)
    {
        if (_cache.TryGetValue(path, out Node node))
            return node;
        else
        {
            Node result = source.GetNode(path);
            if (result is null)
                return null;
            else
                return _cache[path] = result;
        }
    }

    /// <summary>
    /// Fetches a node by <see cref="NodePath"/>. Similar to <see cref="GetNode(NodePath)"/>, but does not generate
    /// an error if path does not point to a valid node.
    /// </summary>
    /// <param name="path">Path to the node to get relative to <see cref="source"/>.</param>
    /// <returns>The <see cref="Node"/> at the given path.</returns>
    public Node GetNodeOrNull(NodePath path)
    {
        if (_cache.TryGetValue(path, out Node node))
            return node;
        else
        {
            Node result = source.GetNodeOrNull(path);
            if (result is null)
                return null;
            else
                return _cache[path] = result;
        }
    }

    /// <summary>
    /// Fetches a node. The <see cref="NodePath"/> can be either a relative path (from the source node) or an absolute
    /// path (in the scene tree) to a node. If the path does not exist, a null instance is returned, an error is logged,
    /// and the result is not cached.
    /// </summary>
    /// <typeparam name="N">Type of <see cref="Node"/> to get.</typeparam>
    /// <param name="path">Path to the node to get relative to <see cref="source"/>.</param>
    /// <returns>The <see cref="Node"/> at the given path.</returns>
    /// <exception cref="System.InvalidCastException">If the node can't be cast.</exception>
    /// <remarks>
    /// Note: Fetching absolute paths only works when the node is inside the scene tree
    /// (see <see cref="Node.IsInsideTree"/>).
    /// </remarks>
    public N GetNode<N>(NodePath path) where N : Node => (N)GetNode(path);

    /// <summary>
    /// Fetches a node by <see cref="NodePath"/>. Similar to <see cref="GetNode(NodePath)"/>, but does not generate
    /// an error if path does not point to a valid node.
    /// </summary>
    /// <typeparam name="N">Type of <see cref="Node"/> to get.</typeparam>
    /// <param name="path">Path to the node to get relative to <see cref="source"/>.</param>
    /// <returns>The <see cref="Node"/> at the given path.</returns>
    public N GetNodeOrNull<N>(NodePath path) where N : Node => GetNodeOrNull(path) as N;
}