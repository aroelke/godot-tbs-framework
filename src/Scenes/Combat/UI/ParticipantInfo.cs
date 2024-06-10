using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using Nodes;
using Nodes.Components;

namespace Scenes.Combat.UI;

/// <summary>Combat UI element that displays constant information about a character participating in combat.</summary>
[Tool]
public partial class ParticipantInfo : GridContainer, IHasHealth
{
    private readonly  NodeCache _cache;
    public ParticipantInfo() : base() => _cache = new(this);

    private int _maxHealth = 10, _currentHealth = 10;
    private int[] _damage = new[] { 0 };
    private int _hit = 0;

    private Label HealthLabel => _cache.GetNodeOrNull<Label>("%HealthLabel");
    private TextureProgressBar HealthBar => _cache.GetNodeOrNull<TextureProgressBar>("%HealthBar");
    private Label DamageTitle => _cache.GetNodeOrNull<Label>("%DamageTitle");
    private Label DamageLabel => _cache.GetNodeOrNull<Label>("%DamageLabel");
    private Label HitChanceTitle => _cache.GetNodeOrNull<Label>("%HitChanceTitle");
    private Label HitChanceLabel => _cache.GetNodeOrNull<Label>("%HitChanceLabel");

    /// <summary>Amount to scale the health value internally so the health bar transitions smoothly.</summary>
    [Export] public float HealthScale = 100;

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

                bool heal = _damage.Length == 1 && _damage[0] < 0;
                if (DamageTitle is not null)
                {
                    DamageTitle.Visible = _damage.Any();
                    if (heal)
                        DamageTitle.Text = "Healing:";
                    else
                        DamageTitle.Text = "Damage:";
                }
                if (DamageLabel is not null)
                {
                    DamageLabel.Visible = _damage.Any();
                    if (heal)
                        DamageLabel.Text = Math.Abs(_damage[0]).ToString();
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
                if (HitChanceTitle is not null)
                    HitChanceTitle.Visible = _hit >= 0;
                if (HitChanceLabel is not null)
                {
                    HitChanceLabel.Visible = _hit >= 0;
                    HitChanceLabel.Text = $"{_hit}%";
                }
            }
        }
    }

    public HealthComponent Health
    {
        get => _cache.GetNode<HealthComponent>("Health");
        set
        {
            HealthComponent health = _cache.GetNodeOrNull<HealthComponent>("Health");
            if (value is not null && health is not null)
            {
                health.Maximum = value.Maximum;
                health.Value = value.Value;

                if (HealthBar is not null)
                {
                    HealthBar.MaxValue = health.Maximum*HealthScale;
                    HealthBar.Value = health.Value*HealthScale;
                }
                if (HealthLabel is not null)
                    HealthLabel.Text = $"HP: {health.Value}";
            }
        }
    }

    /// <summary>Smoothly transition health to a new value.</summary>
    /// <remarks>Note that the actual value of <see cref="CurrentHealth"/> doesn't update until the transition is done.</remarks>
    /// <param name="value">New health value.</param>
    /// <param name="duration">Amount of time to take to transition it.</param>
    public void TransitionHealth(int value, double duration)
    {
        int next = Mathf.Clamp(value, 0, Health.Maximum);
        CreateTween().TweenMethod(Callable.From((float hp) => {
            HealthBar.Value = hp;
            Health.Value = (int)(hp/HealthScale);
        }), Health.Value*HealthScale, next*HealthScale, duration).Finished += () => Debug.Assert(Health.Value == next, "Health does not match value after transitioning.");
    }

    public void OnHealthChanged(int value) =>  HealthLabel.Text = $"HP: {value}";
}