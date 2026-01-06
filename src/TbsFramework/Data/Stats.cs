using System;
using System.Linq;
using Godot;

namespace TbsFramework.Data;

/// <summary>
/// Structure defining the stats of an entity, such as a class or character. Can be added together to
/// create a final stat value for a character from components.
/// </summary>
[GlobalClass, Tool]
public partial class Stats : Resource
{
    public delegate void ValuesChangedEventHandler(Stats stats);

    public static Stats operator+(Stats a, Stats b) => new()
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

    public event ValuesChangedEventHandler ValuesChanged;

    private readonly ObservableProperty<int> _health = 10;
    private readonly ObservableProperty<int> _attack = 1;
    private readonly ObservableProperty<int> _defense = 0;
    private readonly ObservableProperty<int> _healing = 0;
    private readonly ObservableProperty<int> _accuracy = 100;
    private readonly ObservableProperty<int> _evasion = 0;
    private readonly ObservableProperty<int> _agility = 1;
    private readonly ObservableProperty<int> _move = 5;
    private readonly ObservableProperty<int[]> _attackRange = new int[]{ 1 };
    private readonly ObservableProperty<int[]> _supportRange = Array.Empty<int>();

    /// <summary>Max health stat. Determines the amount of damage a unit can take before being defeated.</summary>
    [Export] public int Health
    {
        get => _health.Value;
        set => _health.Value = value;
    }

    /// <summary>Temporary attack stat. Determines the amount of damage a unit deals whenn it attacks.</summary>
    [Export] public int Attack
    {
        get => _attack.Value;
        set => _attack.Value = value;
    }

    /// <summary>Temporary defense stat. Reduce damage taken from attacks.</summary>
    [Export] public int Defense
    {
        get => _defense.Value;
        set => _defense.Value = value;
    }

    /// <summary>Temporary healing stat. Determines amount of HP restored when supporting.</summary>
    [Export] public int Healing
    {
        get => _healing.Value;
        set => _healing.Value = value;
    }

    /// <summary>Temporary accuracy stat. Increases chance of hitting when attacking.</summary>
    [Export] public int Accuracy
    {
        get => _accuracy.Value;
        set => _accuracy.Value = value;
    }

    /// <summary>Temporary evasion stat. Decreases chance of being hit when attacking.</summary>
    [Export] public int Evasion
    {
        get => _evasion.Value;
        set => _evasion.Value = value;
    }

    /// <summary>Temporary agility stat. When higher than an enemy's, allows for a follow-up attack in combat.</summary>
    [Export] public int Agility
    {
        get => _agility.Value;
        set => _agility.Value = value;
    }

    /// <summary>Movement range.</summary>
    [Export] public int Move
    {
        get => _move.Value;
        set => _move.Value = value;
    }

    /// <summary>Distance at which the unit can attack an enemy.</summary>
    /// <remarks>Don't directly change the values of the array elements, as this won't raise <see cref="ValuesChanged"/>.</remarks>
    [Export] public int[] AttackRange
    {
        get => _attackRange.Value;
        set => _attackRange.Value = value;
    }

    /// <summary>Distance at which the unit can support an enemy.</summary>
    /// <remarks>Don't directly change the values of the array elements, as this won't raise <see cref="ValuesChanged"/>.</remarks>
    [Export] public int[] SupportRange
    {
        get => _supportRange;
        set => _supportRange.Value = value;
    }

    public Stats()
    {
        _health.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _attack.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _defense.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _healing.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _accuracy.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _evasion.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _agility.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _move.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _attackRange.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
        _supportRange.ValueChanged += (_, _) => { if (ValuesChanged is not null) ValuesChanged(this); };
    }
}