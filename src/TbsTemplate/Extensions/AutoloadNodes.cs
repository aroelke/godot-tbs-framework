using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Extensions;

/// <summary>Cache for autoload nodes.</summary>
public static class AutoloadNodes
{
    private static readonly Dictionary<NodePath, Node> _autoloads = [];

    /// <summary>Fetches an autoloaded node.</summary>
    /// <typeparam name="T">Type of the node.</typeparam>
    /// <param name="path">Name of the node.</param>
    /// <exception cref="System.InvalidCastException"/>
    public static T GetNode<T>(NodePath path) where T : Node
    {
        if (_autoloads.TryGetValue(path, out Node node))
            return (T)node;
        else
            return (T)(_autoloads[path] = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<T>(path));
    }

    /// <summary>Fetches an autload node, if it exists.</summary>
    /// <typeparam name="T">Type of the node.</typeparam>
    /// <param name="path">Name of the node.</param>
    /// <returns>The autoload node named <paramref name="path"/>, or <c>null</c> if not found.</returns>
    public static T GetNodeOrNull<T>(NodePath path) where T : Node
    {
        if (_autoloads.TryGetValue(path, out Node node))
            return node as T;
        else
            return (_autoloads[path] = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<T>(path)) as T;
    }
}