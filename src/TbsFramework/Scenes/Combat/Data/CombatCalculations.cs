using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Combat.Data;

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

    /// <summary>Create an action representing the result of a single attack.</summary>
    /// <param name="attacker">Unit performing the attack.</param>
    /// <param name="defender">Unit receiving the attack.</param>
    public static CombatAction CreateAttackAction(Unit attacker, Unit defender) => new(attacker, defender, CombatActionType.Attack, Damage(attacker, defender), rnd.Next(100) < HitChance(attacker, defender));

    /// <summary>Create an action representing the result of a single support.</summary>
    /// <param name="supporter">Unit performing the support.</param>
    /// <param name="recipient">Unit receiving the support.</param>
    public static CombatAction CreateSupportAction(Unit supporter, Unit recipient) => new(supporter, recipient, CombatActionType.Support, -supporter.Stats.Healing, true);

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

    /// <summary>Compute the results of a combat between two <see cref="Unit"/>s, ignoring whether or not either of the units dies.</summary>
    /// <param name="a">One of the participants.</param>
    /// <param name="b">One of the participants.</param>
    /// <returns>A list of data structures specifying the action taken during each round of combat.</returns>
    public static List<CombatAction> AttackResults(Unit a, Unit b)
    {
        Dictionary<Unit, int> damage = new() {{ a, 0 }, { b, 0 }};
        // Compute complete combat action list
        List<CombatAction> actions = [CreateAttackAction(a, b)];
        damage[b] += actions[^1].Damage;
        if (damage[b] < b.Health.Value && b.AttackableCells().Contains(a.Cell))
        {
            actions.Add(CreateAttackAction(b, a));
            damage[a] += actions[^1].Damage;
        }
        if (FollowUp(a, b) is (Unit doubler, Unit doublee) && damage[doubler] < doubler.Health.Value && doubler.AttackableCells().Contains(doublee.Cell))
        {
            actions.Add(CreateAttackAction(doubler, doublee));
            damage[doublee] += actions[^1].Damage;
        }

        return actions;
    }

    /// <summary>Compute the total amount of damage dealt to a participant in combat</summary>
    /// <param name="target">Participant to compute damage for.</param>
    /// <param name="actions">Actions describing what happened in combat.</param>
    /// <returns>The total amount of damage dealt to <paramref name="target"/> based on <paramref name="actions"/>.</returns>
    public static int TotalDamage(Unit target, IEnumerable<CombatAction> actions) => actions.Where((a) => a.Hit && a.Target == target).Select((a) => a.Damage).Sum();

    /// <summary>Compute the expected amount of damage dealt to a participant in combat (e.g. sum of damage of each hit multiplied by its accuracy).</summary>
    /// <param name="target">Participant to compute damage for.</param>
    /// <param name="actions">Actions describing what will happen in combat.</param>
    /// <returns>The total expected amount of damage, in a statistical sense, <paramref name="target"/> will receive based on <paramref name="actions"/>.</returns>
    public static double TotalExpectedDamage(Unit target, IEnumerable<CombatAction> actions) => actions.Where((a) => a.Target == target).Select((a) => a.Damage*HitChance(a.Actor, target)/100.0).Sum();
}