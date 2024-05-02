using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace Nodes;

/// <summary>Caches accesses of <see cref="Node"/>s via <see cref="NodePath"/>s so they can be reaccessed later without searching the scene tree.</summary>
public class NodeCache
{
    private readonly Node _owner;
    private readonly Dictionary<NodePath, object> _children = new();

    /// <summary>Create a new cache for <see cref="Node"/>s that are children of a specified <see cref="Node"/>.</summary>
    /// <param name="owner">Parent of the <see cref="Node"/>s to cache.</param>
    public NodeCache([NotNull] Node owner)
    {
        _owner = owner;
        if (!Engine.IsEditorHint())
        {
            _owner.ChildExitingTree += (Node node) => {
                if (_children.ContainsValue(node))
                    foreach ((NodePath path, object child) in _children)
                        if (child == node)
                            _children.Remove(path);
            };
        }
    }

    /// <inheritdoc cref="Node.GetNode{T}(NodePath)"/>
    public T GetNode<T>(NodePath path) where T : class
    {
        if (Engine.IsEditorHint())
            return _owner.GetNode<T>(path);
        else
        {
            if (!_children.ContainsKey(path))
                _children[path] = _owner.GetNode<T>(path);
            return (T)_children[path];
        }
    }

    /// <inheritdoc cref="Node.GetNode(NodePath)"/>
    public Node GetNode(NodePath path) => Engine.IsEditorHint() ? _owner.GetNode(path) : GetNode<Node>(path);

    /// <inheritdoc cref="Node.GetNodeOrNull{T}(NodePath)"/>
    /// <param name="storeNull">Whether or not to cache a <c>null</c> result.</param>
    public T GetNodeOrNull<T>(NodePath path, bool storeNull=false) where T : class
    {
        if (Engine.IsEditorHint())
            return _owner.GetNodeOrNull<T>(path);
        else
        {
            if (!_children.ContainsKey(path))
            {
                T child = _owner.GetNodeOrNull<T>(path);
                if (child is null && !storeNull)
                    return null;
                _children[path] = child;
            }
            return (T)_children[path];
        }
    }

    /// <inheritdoc cref="Node.GetNodeOrNull(NodePath)"/>
    /// <param name="storeNull">Whether or not to cache a <c>null</c> result.</param>
    public Node GetNodeOrNull(NodePath path, bool storeNull=false) => Engine.IsEditorHint() ? _owner.GetNodeOrNull(path) : GetNodeOrNull<Node>(path, storeNull);
}