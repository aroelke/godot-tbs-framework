using System;
using System.Diagnostics.CodeAnalysis;

using Godot;

namespace GD_NET_ScOUT;

// SceneScout does not have the [Tool] attribute, so null-propagation is error-prone
[SuppressMessage("ReSharper", "UseNullPropagation")]
public static class Extensions
{
    private const string AutoloadNode = $"/root/{SceneScout.AutoloadName}";

    public static TestRunner GetTestRunner(this Node node)
    {
        return GetSceneScout(node)?.CurrentTestRunner
            ?? throw new ApplicationException("Test runner not found.");
    }

    internal static SceneScout? GetSceneScout(this Node node)
    {
        Node? autoload = node.GetNodeOrNull(AutoloadNode);
        if (autoload is null)
        {
            return null;
        }

        return autoload as SceneScout;
    }

    internal static int GodotPathLen(this string s) => s.IndexOf(':') + 3;
}
