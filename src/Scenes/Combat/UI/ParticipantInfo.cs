using System;
using System.Linq;
using Godot;
using Nodes;

namespace Scenes.Combat.UI;

/// <summary>Combat UI element that displays constant information about a unit participating in combat.</summary>
[Tool]
public partial class ParticipantInfo : VBoxContainer
{
    private readonly  NodeCache _cache;
    public ParticipantInfo() : base() => _cache = new(this);

    private int[] _damage = new[] { 0 };
    private int _hit = 0;
    private HorizontalAlignment _alignment = HorizontalAlignment.Left;

    private Label DamageLabel => _cache.GetNodeOrNull<Label>("%DamageLabel");
    private Label HitChanceLabel => _cache.GetNodeOrNull<Label>("%HitChanceLabel");

    /// <summary>Amount of damage each action will deal. Use a negative number to indicate healing. Use an empty array to hide, e.g. for buffing.</summary>
    /// <exception cref="ArgumentException">If a damage sequence contains both positive (damage) and negative (healing) values.</exception>
    [Export] public int[] Damage
    {
        get => _damage;
        set
        {
            if (value.Any() && ((value[0] < 0 && value.Any((x) => x >= 0)) || (value[0] >= 0 && value.Any((x) => x < 0))))
                throw new ArgumentException($"Combat contains damage values with mixed signs: {string.Join(",", value)}");

            if (_damage != value)
            {
                _damage = value;
                if (DamageLabel is not null)
                {
                    DamageLabel.Visible = _damage.Any();
                    if (_damage.Length == 1 && _damage[0] < 0)
                        DamageLabel.Text = $"Healing: {_damage[0]}";
                    else
                        DamageLabel.Text = $"Damage: {string.Join(" + ", _damage)}";
                }
            }
        }
    }

    /// <summary>Chance an action will hit. If this doesn't apply, such as for healing, use a negative number to hide.</summary>
    [Export] public int HitChance
    {
        get => _hit;
        set
        {
            if (_hit != value)
            {
                _hit = value;
                if (HitChanceLabel is not null)
                {
                    HitChanceLabel.Visible = _hit >= 0;
                    HitChanceLabel.Text = $"Hit Chance: {_hit}%";
                }
            }
        }
    }

    /// <summary>Horizontal alignment of text elements.</summary>
    [Export] public HorizontalAlignment HorizontalAlignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                if (DamageLabel is not null)
                    DamageLabel.HorizontalAlignment = _alignment;
                if (HitChanceLabel is not null)
                    HitChanceLabel.HorizontalAlignment = _alignment;
            }
        }
    }
}