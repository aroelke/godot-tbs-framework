using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Properties;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Demo;

/// <summary>
/// Structure defining the stats of an entity, such as a class or character. Can be added together to
/// create a final stat value for a character from components.
/// </summary>
[GlobalClass, Tool]
public partial class DemoStats : AbstractStats
{
    public static DemoStats operator+(DemoStats a, DemoStats b) => new()
    {
        Health       = a.Health   + b.Health,
        Attack       = a.Attack   + b.Attack,
        Defense      = a.Defense  + b.Defense,
        Healing      = a.Healing  + b.Healing,
        Accuracy     = a.Accuracy + b.Accuracy,
        Evasion      = a.Evasion  + b.Evasion,
        Agility      = a.Agility  + b.Agility,
        Move         = a.Move     + b.Move,
        AttackRange  = [.. a.AttackRange.Concat(b.AttackRange).Distinct().Order()],
        SupportRange = [.. a.SupportRange.Concat(b.SupportRange).Distinct().Order()]
    };

    /// <summary>Max health stat. Determines the amount of damage a unit can take before being defeated.</summary>
    [Export] public int Health = 10;

    /// <summary>Temporary attack stat. Determines the amount of damage a unit deals whenn it attacks.</summary>
    [Export] public int Attack = 1;

    /// <summary>Temporary defense stat. Reduce damage taken from attacks.</summary>
    [Export] public int Defense = 0;

    /// <summary>Temporary healing stat. Determines amount of HP restored when supporting.</summary>
    [Export] public int Healing = 0;

    /// <summary>Temporary accuracy stat. Increases chance of hitting when attacking.</summary>
    [Export] public int Accuracy = 100;

    /// <summary>Temporary evasion stat. Decreases chance of being hit when attacking.</summary>
    [Export] public int Evasion = 0;

    /// <summary>Temporary agility stat. When higher than an enemy's, allows for a follow-up attack in combat.</summary>
    [Export] public int Agility = 1;

    /// <summary>Movement range.</summary>
    [Export] public int Move = 5;

    /// <summary>Distance at which the unit can attack an enemy.</summary>
    /// <remarks>Don't directly change the values of the array elements, as this won't raise <see cref="DemoValuesChanged"/>.</remarks>
    [Export] public int[] AttackRange = [1];

    /// <summary>Distance at which the unit can support an enemy.</summary>
    /// <remarks>Don't directly change the values of the array elements, as this won't raise <see cref="DemoValuesChanged"/>.</remarks>
    [Export] public int[] SupportRange = [];

    /// <summary>Cost modifiers to move onto different types of terrain.</summary>
    /// <remark>Don't directly change the value of the dictionary elements, as this won't raise <see cref="DemoValuesChanged"/>.</remarks>
    [Export] public Godot.Collections.Dictionary<Terrain, int> TerrainCostModifiers = [];

    public override double MaxHealth => Health;
    public override int MoveDistance => Move;

    public override int GetTerrainCostModifier(Terrain terrain) => TerrainCostModifiers.GetValueOrDefault(terrain, 0);
}