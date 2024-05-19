using Scenes.Level.Object;

namespace Scenes.Combat;

/// <summary>Data structure holding the information needed to choreograph one turn of combat.</summary>
public struct CombatAction
{
    /// <summary>Which unit is acting this turn.</summary>
    public Unit Actor;

    /// <summary>Which unit is the target of the action this turn.</summary>
    public Unit Target;

    /// <summary>How much damage is deal this turn. Use a negative number to indicate healing.  Zero does not mean the attack misses.</summary>
    public int Damage;

    /// <summary>Whether or not the attack hits.</summary>
    public bool Hit;
}