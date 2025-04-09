using TbsTemplate.Scenes.Level.State.Occupants;

namespace TbsTemplate.Scenes.Combat.Data;

/// <summary>Types of actions that can be performed in a combat scenario.</summary>
public enum CombatActionType
{
    /// <summary>The acting unit is attacking.</summary>
    Attack,
    /// <summary>The acting unit is supporting.</summary>
    Support
}

/// <summary>Data structure holding the information needed to choreograph one turn of combat.</summary>
/// <param name="Actor">Which unit is acting this turn.</param>
/// <param name="Target">Which unit is the target of the action this turn.</param>
/// <param name="Type">Type of action <paramref name="Actor"/> is performing on <paramref name="Target"/>.</param>
/// <param name="Damage">How much damage is deal this turn. Use a negative number to indicate healing.  Zero does not mean the attack misses.</param>
/// <param name="Hit">Whether or not the attack hits.</param>
public readonly record struct CombatAction(UnitState Actor, UnitState Target, CombatActionType Type, int Damage, bool Hit) {}