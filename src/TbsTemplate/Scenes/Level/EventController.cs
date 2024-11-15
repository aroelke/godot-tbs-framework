using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level;

/// <summary>
/// Object controlling scriptable aspects of a level.  Interacts with the <see cref="LevelManager"/> to advance the state of the level.
/// </summary>
public partial class EventController : Node
{
    /// <summary>Signal that an event is complete.</summary>
    [Signal] public delegate void EventCompleteEventHandler();

    /// <summary>
    /// Event to perform before an army's turn begins. By default, just signals to start the turn. Overriding classes should signal <c>EventComplete</c>
    /// at the end of their handlers.
    /// </summary>
    /// <param name="turn">Turn number that's about to begin.</param>
    /// <param name="army">Army that's about to begin its turn.</param>
    public virtual void OnTurnBegan(int turn, Army army) => EmitSignal(SignalName.EventComplete);

    /// <summary>
    /// Event to perform just after a unit ends its action. By default, just signals to continue to the next action. Override classes should signal
    /// <c>EventComplete</c> at the end of their handlers.
    /// </summary>
    /// <param name="unit">Unit that just acted.</param>
    public virtual void OnActionEnded(Unit unit) => EmitSignal(SignalName.EventComplete);
}