using System;
using System.Collections.Immutable;
using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Combat;

public abstract partial class CombatController : Node
{
    /// <summary>Signal that combat has ended.</summary>
    [Signal] public delegate void CombatEndedEventHandler();

    /// <summary>Set up the combat.</summary>
    /// <param name="left">Unit on the left side of the screen.</param>
    /// <param name="right">Unit on the right side of the screen.</param>
    /// <param name="actions">List of actions that will be performed each turn in combat. The length of the list determines the number of turns.</param>
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains a unit who isn't participating in this combat.</exception>
    public virtual void Initialize(UnitData left, UnitData right, IImmutableList<CombatAction> actions)
    {
        foreach (CombatAction action in actions)
            if (action.Actor != left && action.Actor != right)
                throw new ArgumentException($"Unit at cell {action.Actor.Cell} is not a participant in combat");
    }

    /// <summary>Begin the combat animation sequence.</summary>
    public abstract void Start();

    /// <summary>Initiate the end of the combat. This can be used to trigger returning to the map that initiated the combat so the level can continue.</summary>
    public abstract void End();
}