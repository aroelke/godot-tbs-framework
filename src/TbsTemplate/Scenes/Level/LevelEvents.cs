using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level;

/// <summary>"Event bus" for a level that allows objects to subscribe to various events that occur during play.</summary>
public partial class LevelEvents : Node
{
    private static LevelEvents _singleton = null;

    /// <summary>
    /// Auto-loaded instance of <see cref="LevelEvents"/> in case instances methods are required. Signal connection methods are statically
    /// overriden.
    /// </summary>
    public static LevelEvents Singleton => _singleton ??= ((SceneTree)Engine.GetMainLoop()).Root.GetNode<LevelEvents>("LevelEvents");

    /// <inheritdoc cref="GodotObject.EmitSignal(StringName, Variant[])"/>
    /// <remarks>Emits the signal from the singleton/></remarks>
    public static new Error EmitSignal(StringName signal, params Variant[] args) => ((GodotObject)Singleton).EmitSignal(signal, args);

    /// <inheritdoc cref="GodotObject.EmitSignal(StringName, Variant[])"/>
    /// <remarks>Connects to a signal in the singleton.</remarks>
    public static new Error Connect(StringName signal, Callable callable, uint flags=0) => ((GodotObject)Singleton).Connect(signal, callable, flags);

#region Level Manager
    /// <summary>Signals that an army's turn has begun.</summary>
    /// <param name="turn">Number of the turn that began.</param>
    /// <param name="army">Army whose turn began.</param>
    [Signal] public delegate void TurnBeganEventHandler(int turn, Army army);

    /// <summary>Signals that a unit's action has ended.</summary>
    [Signal] public delegate void ActionEndedEventHandler(Unit unit);
#endregion
#region Event Controller
    /// <summary>Signal that an event is complete, so the level can stop waiting for it.</summary>
    [Signal] public delegate void EventCompleteEventHandler();
#endregion
}