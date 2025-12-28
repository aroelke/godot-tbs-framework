using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Combat.Data;

/// <summary>Types of actions that can be performed in a combat scenario.</summary>
public enum CombatActionType
{
    /// <summary>The acting unit is attacking.</summary>
    Attack,
    /// <summary>The acting unit is supporting.</summary>
    Support
}

/// <summary>Data structure holding the information needed to choreograph one action of combat.</summary>
/// <param name="Actor">Which unit is acting.</param>
/// <param name="Target">Which unit is the target of the action.</param>
/// <param name="Type">Type of action <paramref name="Actor"/> is performing on <paramref name="Target"/>.</param>
/// <param name="Damage">How much damage is dealt. Use a negative number to indicate healing.  Zero does not mean the attack misses.</param>
/// <param name="Hit">Whether or not the attack hits.</param>
public readonly record struct CombatAction(IUnit Actor, IUnit Target, CombatActionType Type, double Damage, bool Hit) {}