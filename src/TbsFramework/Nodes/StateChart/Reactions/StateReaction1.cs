using Godot;

namespace TbsFramework.Nodes.StateCharts.Reactions;

/// <summary>Reaction that has a single parameter.</summary>
/// <typeparam name="T">Type of the parameter. Must be a variant type.</typeparam>
/// <param name="signal">Name of the signal to emit when reacting.</param>
public abstract partial class StateReaction1<[MustBeVariant] T>(StringName signal) : StateReaction
{
    public void React(T value)
    {
        if (Active)
            EmitSignal(signal, Variant.From(value));
    }
}