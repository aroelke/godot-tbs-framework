using System.Collections.Immutable;
using Godot;
using TbsTemplate.Scenes.Combat.Data;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Combat;

/// <summary>Scene used to display the results of combat in a cut scene.</summary>
public abstract partial class CombatScene : Node
{
    /// <summary>Set up the combat.</summary>
    /// <param name="left">Unit on the left side of the screen.</param>
    /// <param name="right">Unit on the right side of the screen.</param>
    /// <param name="actions">List of actions that will be performed each turn in combat. The length of the list determines the number of turns.</param>
    /// <exception cref="ArgumentException">If any <see cref="CombatAction"/> contains a unit who isn't participating in this combat.</exception>
    public abstract void Initialize(Unit left, Unit right, IImmutableList<CombatAction> actions);
}