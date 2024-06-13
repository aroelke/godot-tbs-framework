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

    /// <summary>Amount of time to take to update the health bar when health is changed.</summary>
    [Export(PropertyHint.None, "suffix:s")] public double TransitionDuration = 0.3;

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
                    HealthBar.MaxValue = health.Maximum;
                    HealthBar.Value = health.Value;
                }
                if (HealthLabel is not null)
                    HealthLabel.Text = $"HP: {health.Value}";
            }
        }
    }

    public void OnHealthChanged(float value)
    {
        void UpdateHealth(float hp)
        {
            HealthBar.Value = (int)hp;
            HealthLabel.Text = $"HP: {(int)hp}";
        }

        GD.Print(IsInsideTree());

        if (!Engine.IsEditorHint() && IsInsideTree())
            CreateTween().TweenProperty(HealthBar, new(TextureProgressBar.PropertyName.Value), value, TransitionDuration);
        else
            UpdateHealth(value);
    }
}