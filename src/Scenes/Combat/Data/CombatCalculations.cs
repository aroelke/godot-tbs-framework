using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Scenes.Level.Object;

namespace Scenes.Combat.Data;

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
    public static int Damage(Unit attacker, Unit defender) => Math.Max(attacker.Stats.Attack - defender.Stats.Defense, 0);

    /// <summary>
    /// Determine the probability that an attacker's attack will land. The formula is currently:
    /// <code>
    /// hit chance = attacker.accuracy - defender.evasion
    /// </code>
    /// </summary>
    /// <param name="attacker">Attacking unit.</param>
    /// <param name="defender">Defending unit.</param>
    /// <returns>A number from 0 through 100 that indicates the chance, in percent that the attacker's attack will hit.</returns>
    public static int HitChance(Unit attacker, Unit defender) => attacker.Stats.Accuracy - defender.Stats.Evasion;

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
    public static (Unit doubler, Unit other)? FollowUp(Unit a, Unit b) => (a.Stats.Agility - b.Stats.Agility) switch
    {
        >0 => (a, b),
        <0 => (b, a),
        _  => null
    };

    /// <summary>Compute the results of a combat between two <see cref="Unit"/>s.</summary>
    /// <param name="a">One of the participants.</param>
    /// <param name="b">One of the participants.</param>
    /// <returns>A list of data structures specifying the action taken during each round of combat.</returns>
    public static ImmutableList<CombatAction> CombatResults(Unit a, Unit b)
    {
        ImmutableList<CombatAction> actions = ImmutableList.Create<CombatAction>(
            new() { Actor = a, Target = b, Damage = Damage(a, b), Hit = rnd.Next(100) < HitChance(a, b) },
            new() { Actor = b, Target = a, Damage = Damage(b, a), Hit = rnd.Next(100) < HitChance(b, a) }
        );
        if (FollowUp(a, b) is (Unit doubler, Unit doublee))
            return actions.Add(new() { Actor = doubler, Target = doublee, Damage = Damage(doubler, doublee), Hit = rnd.Next(100) < HitChance(doubler, doublee) });
        else
            return actions;
    }

    public static int TotalDamage(Unit target, IEnumerable<CombatAction> actions) => actions.Where((a) => a.Hit && a.Target == target).Select((a) => a.Damage).Sum();
}