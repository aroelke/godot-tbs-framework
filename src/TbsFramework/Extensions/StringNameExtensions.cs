#nullable enable

using System;
using System.Linq;
using Godot;

namespace TbsFramework.Extensions;

/// <summary>Extensions for <see cref="StringName"/>.</summary>
public static class StringNameExtensions
{
    /// <summary>Split a <see cref="StringName"/> into sub-<see cref="StringName"/>s using the provided separator.</summary>
    /// <returns>An array whose elements contain the sub-<see cref="StringName"/>s from the instance that are delimited by <paramref name="separator"/>.</returns>
    public static StringName[] Split(this StringName s, string? separator, StringSplitOptions options=StringSplitOptions.None) => s.ToString().Split(separator, options).Select(static (t) => new StringName(t)).ToArray();
}