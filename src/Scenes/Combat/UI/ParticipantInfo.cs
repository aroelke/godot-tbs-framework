using System;
using System.Linq;
using Godot;
using Nodes;

namespace Scenes.Combat.UI;

/// <summary>Combat UI element that displays constant information about a character participating in combat.</summary>
[Tool]
public partial class ParticipantInfo : GridContainer
{
    private readonly  NodeCache _cache;
    public ParticipantInfo() : base() => _cache = new(this);

    private int _maxHealth = 10, _currentHealth = 10;
    private int[] _damage = new[] { 0 };
    private int _hit = 0;

    private Label HealthLabel => _cache.GetNodeOrNull<Label>("%HealthLabel");
    private TextureProgressBar HealthBar => _cache.GetNodeOrNull<TextureProgressBar>("%HealthBar");
    private Label DamageLabel => _cache.GetNodeOrNull<Label>("%DamageLabel");
    private Label HitChanceLabel => _cache.GetNodeOrNull<Label>("%HitChanceLabel");

    /// <summary>Max health of the character.</summary>
    [Export] public int MaxHealth
    {
        get => _maxHealth;
        set
        {
            int next = Mathf.Max(0, value);
            if (_maxHealth != next)
            {
                _maxHealth = next;
                if (HealthBar is not null)
                    HealthBar.MaxValue = _maxHealth;

                if (CurrentHealth > _maxHealth)
                    CurrentHealth = _maxHealth;
            }
        }
    }

    /// <summary>"Current" health of the character.</summary>
    /// <remarks>The character's actual current health is updated elsewhere; this is just for display purposes.</remarks>
    [Export] public int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            int next = Mathf.Clamp(value, 0, MaxHealth);
            if (_currentHealth != next)
            {
                _currentHealth = next;
                if (HealthLabel is not null)
                    HealthLabel.Text = $"HP: {_currentHealth}";
                if (HealthBar is not null)
                    HealthBar.Value = _currentHealth;
            }
        }
    }

    /// <summary>Amount of damage each action will deal. Use a negative number to indicate healing. Use an empty array to hide, e.g. for buffing.</summary>
    /// <exception cref="ArgumentException">If a damage sequence contains both positive (damage) and negative (healing) values.</exception>
    [Export] public int[] Damage
    {
        get => _damage;
        set
        {
            if (value.Any() && ((value[0] < 0 && value.Any((x) => x >= 0)) || (value[0] >= 0 && value.Any((x) => x < 0))))
            {
                string error = $"Combat contains damage values with mixed signs: {string.Join(",", value)}";
                if (Engine.IsEditorHint())
                    GD.PushError(error);
                else
                    throw new ArgumentException(error);
            }

            if (_damage != value)
            {
                _damage = value;
                if (DamageLabel is not null)
                {
                    DamageLabel.Visible = _damage.Any();
                    if (_damage.Length == 1 && _damage[0] < 0)
                        DamageLabel.Text = _damage[0].ToString();
                    else
                        DamageLabel.Text = string.Join(" + ", _damage);
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
                    HitChanceLabel.Text = $"{_hit}%";
                }
            }
        }
    }
}