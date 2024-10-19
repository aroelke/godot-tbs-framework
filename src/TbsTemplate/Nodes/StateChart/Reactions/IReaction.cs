using Godot;

namespace TbsTemplate.Nodes.StateChart.Reactions;

/// <summary>Reaction interface for reactions with no parameters.</summary>
public interface IReaction
{
    /// <summary>Perform an action in response to an event.</summary>
    public void React();
}

/// <summary>Reaction interface for reactions with a single parameter.</summary>
/// <typeparam name="T">Type of the parameter.</typeparam>
public interface IReaction<[MustBeVariant] T>
{
    /// <summary>Perform an action in response to an event with a single value.</summary>
    /// <param name="value">Value of the event.</param>
    public void React(T value);
}