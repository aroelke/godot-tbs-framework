using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Reaction that has two parameters.</summary>
/// <typeparam name="T">First parameter type. Must be variant compatible.</typeparam>
/// <typeparam name="U">Second parameter type. Must be variant compatible.</typeparam>
/// <param name="signal">Name of the signal to emit when reacting.</param>
public abstract partial class Reaction2<[MustBeVariant] T, [MustBeVariant] U>(StringName signal) : Reaction
{
    public void React(T value0, U value1)
    {
        if (Active)
            EmitSignal(signal, Variant.From(value0), Variant.From(value1));
    }
}