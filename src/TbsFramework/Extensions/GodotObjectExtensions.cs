using System;
using System.Threading.Tasks;
using Godot;

namespace TbsFramework.Extensions;

/// <summary>Extensions for <see cref="GodotObject"/>.</summary>
public static class GodotObjectExtensions
{
    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static Error Connect(this GodotObject obj, StringName signal, Action action, uint flags=0) => obj.Connect(signal, Callable.From(action), flags);

    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static Error Connect<[MustBeVariant] T>(this GodotObject obj, StringName signal, Action<T> action, uint flags=0) => obj.Connect(signal, Callable.From(action), flags);

    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static Error Connect<[MustBeVariant] T, [MustBeVariant] U>(this GodotObject obj, StringName signal, Action<T, U> action, uint flags=0) =>
        obj.Connect(signal, Callable.From(action), flags);

    /// <returns>A task that can be awaited that completes when the specified signal is raised.</returns>
    public static async Task AwaitSignal(this GodotObject @this, GodotObject source, StringName signal) => await @this.ToSignal(source, signal);
}