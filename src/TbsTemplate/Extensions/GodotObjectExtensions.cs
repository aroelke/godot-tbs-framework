using System;
using Godot;

namespace TbsTemplate.Extensions;

/// <summary>Extensions for <see cref="GodotObject"/>.</summary>
public static class GodotObjectExtensions
{
    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static Error Connect(this GodotObject obj, StringName signal, Action action, uint flags=0) => obj.Connect(signal, Callable.From(action), flags);

    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static Error Connect<[MustBeVariant] T>(this GodotObject obj, StringName signal, Action<T> action, uint flags=0) => obj.Connect(signal, Callable.From(action), flags);
}