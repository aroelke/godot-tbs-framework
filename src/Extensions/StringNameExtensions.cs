#nullable enable

using System;
using System.Linq;
using Godot;

namespace Extensions;

/// <summary>Extensions for <c>StringName</c>.</summary>
public static class StringNameExtensions
{
    /// <summary>Split a <c>StringName</c> into sub-<c>StringName</c>s using the provided separator.</summary>
    /// <returns>An array whose elements contain the sub-<c>StringName</c>s from the instance that are delimited by the separator.</returns>
    public static StringName[] Split(this StringName s, string? separator, StringSplitOptions options=StringSplitOptions.None) => s.ToString().Split(separator, options).Select((t) => new StringName(t)).ToArray();
}