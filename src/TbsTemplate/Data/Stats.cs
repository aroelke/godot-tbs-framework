using System.Linq;
using Godot;

namespace TbsTemplate.Data;

/// <summary>
/// Structure defining the stats of an entity, such as a class or character. Can be added together to
/// create a final stat value for a character from components.
/// </summary>
[GlobalClass, Tool]
public partial class Stats : Resource
{
    public static Stats operator+(Stats a, Stats b) => new()
    {
        Health = a.Health + b.Health,
        Attack = a.Attack + b.Attack,
        AttackRange = [.. a.AttackRange.Concat(b.AttackRange).Order()],
        Defense = a.Defense + b.Defense,
        Healing = a.Healing + b.Healing,
        SupportRange = [.. a.SupportRange.Concat(b.SupportRange).Order()],
        Accuracy = a.Accuracy + b.Accuracy,
        Evasion = a.Evasion + b.Evasion,
        Agility = a.Agility + b.Agility,
        Move = a.Move + b.Move
    };

    /// <summary>Max health stat. Determines the amount of damage a unit can take before being defeated.</summary>
    [Export] public int Health = 10;

    /// <summary>Temporary attack stat. Determines the amount of damage a unit deals whenn it attacks.</summary>
    [Export] public int Attack = 1;

    /// <summary>Distances from the unit's occupied cell that it can attack.</summary>
    [Export] public int[] AttackRange = [1];

    /// <summary>Temporary defense stat. Reduce damage taken from attacks.</summary>
    [Export] public int Defense = 0;

    /// <summary>Temporary healing stat. Determines amount of HP restored when supporting.</summary>
    [Export] public int Healing = 0;

    /// <summary>Distances from the unit's occupied cell that it can support.</summary>
    [Export] public int[] SupportRange = [];

    /// <summary>Temporary accuracy stat. Increases chance of hitting when attacking.</summary>
    [Export] public int Accuracy = 100;

    /// <summary>Temporary evasion stat. Decreases chance of being hit when attacking.</summary>
    [Export] public int Evasion = 0;

    /// <summary>Temporary agility stat. When higher than an enemy's, allows for a follow-up attack in combat.</summary>
    [Export] public int Agility = 1;

    /// <summary>Movement range.</summary>
    [Export] public int Move = 5;
}