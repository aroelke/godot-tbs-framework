using System;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Combat;

/// <summary>Utility for computing combat stats and results.</summary>
public static class CombatCalculations
{
    private static readonly Random rnd = new();

    /// <summary>
    /// Determine how much damage an attacking <see cref="Unit"/> will deal to a defending one. The formula is currently:
    /// <code>
    /// damage = attacker.attack - defender.defense
    /// </code>
    /// </summary>
    /// <param name="attacker">Attacking unit.</param>
    /// <param name="defender">Defending unit.</param>
    /// <returns>The amount of damage the attacker deals to the defender, with a minimum of 0.</returns>
    public static int Damage(UnitData attacker, UnitData defender) => Math.Max(attacker.Stats.Attack - defender.Stats.Defense, 0);

    /// <summary>
    /// Determine the probability that an attacker's attack will land. The formula is currently:
    /// <code>
    /// hit chance = attacker.accuracy - defender.evasion
    /// </code>
    /// </summary>
    /// <param name="attacker">Attacking unit.</param>
    /// <param name="defender">Defending unit.</param>
    /// <returns>A number from 0 through 100 that indicates the chance, in percent that the attacker's attack will hit.</returns>
    public static int HitChance(UnitData attacker, UnitData defender) => attacker.Stats.Accuracy - defender.Stats.Evasion;

    /// <summary>Create an action representing the result of a single attack.</summary>
    /// <param name="attacker">Unit performing the attack.</param>
    /// <param name="defender">Unit receiving the attack.</param>
    /// <param name="estimate">Whether to average damage based on hit chance (<c>true</c>) or use full damage but have a chance to miss (<c>false</c>).</param>
    public static CombatAction CreateAttackAction(UnitData attacker, UnitData defender, bool estimate) => new(
        attacker,
        defender,
        CombatActionType.Attack,
        Damage(attacker, defender)*(estimate ? (double)HitChance(attacker, defender)/100.0 : 1),
        estimate || rnd.Next(100) < HitChance(attacker, defender)
    );

    /// <summary>Create an action representing the result of a single support.</summary>
    /// <param name="supporter">Unit performing the support.</param>
    /// <param name="recipient">Unit receiving the support.</param>
    public static CombatAction CreateSupportAction(UnitData supporter, UnitData recipient) => new(supporter, recipient, CombatActionType.Support, -Math.Min(supporter.Stats.Healing, recipient.Stats.Health - recipient.Health), true);

    /// <summary>
    /// Deterine which unit, if any, will follow up in a combat situation. Currently, a unit follows up if its agility is higher than the
    /// other participant's.
    /// </summary>
    /// <param name="a">One of the units in combat.</param>
    /// <param name="b">One of the units in combat.</param>
    /// <returns>
    /// If a unit will follow up, a tuple where <c>doubler</c> contains the unit that follows up and <c>other</c> contains the other unit.
    /// If no unit follows up, <c>null</c>.
    /// </returns>
    public static (UnitData doubler, UnitData other)? FollowUp(UnitData a, UnitData b) => (a.Stats.Agility - b.Stats.Agility) switch
    {
        >0 => (a, b),
        <0 => (b, a),
        _  => null
    };
}