using System;
using System.Linq;
using Godot;
using TbsFramework.Nodes.Components;

namespace TbsFramework.Demo;

/// <summary>Combat UI element that displays constant information about a unit participating in combat.</summary>
[Tool]
public partial class CombatantData : GridContainer
{
    private readonly NodeCache _cache = null;
    private int _maxHealth = 10, _currentHealth = 10;
    private int[] _damage = [0];
    private int _hit = 0;

    private HealthComponent    HealthComponent => _cache.GetNode<HealthComponent>("HealthComponent");
    private Label              HealthLabel     => _cache.GetNode<Label>("HealthLabel");
    private Label              DamageTitle     => _cache.GetNode<Label>("DamageTitle");
    private Label              DamageLabel     => _cache.GetNode<Label>("DamageLabel");
    private TextureProgressBar HealthBar       => _cache.GetNode<TextureProgressBar>("HealthBar");
    private Label              HitChanceTitle  => _cache.GetNode<Label>("HitChanceTitle");
    private Label              HitChanceLabel  => _cache.GetNode<Label>("HitChanceLabel");

    /// <summary>Amount of damage each action will deal. Use a negative number to indicate healing. Use an empty array to hide, e.g. for buffing.</summary>
    /// <exception cref="ArgumentException">If a damage sequence contains both positive (damage) and negative (healing) values.</exception>
    [Export] public int[] Damage
    {
        get => _damage;
        set
        {
            if (value.Length != 0 && ((value[0] < 0 && value.Any(static (x) => x >= 0)) || (value[0] >= 0 && value.Any(static (x) => x < 0))))
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
                    DamageTitle.Visible = _damage.Length != 0;
                    if (heal)
                        DamageTitle.Text = "Healing:";
                    else
                        DamageTitle.Text = "Damage:";
                }
                if (DamageLabel is not null)
                {
                    DamageLabel.Visible = _damage.Length != 0;
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
        get => HealthComponent;
        set
        {
            if (value is not null && HealthComponent is not null)
            {
                HealthComponent.Maximum = value.Maximum;
                HealthComponent.Value = value.Value;

                if (HealthBar is not null)
                {
                    HealthBar.MaxValue = HealthComponent.Maximum;
                    HealthBar.Value = HealthComponent.Value;
                }
                if (HealthLabel is not null)
                    HealthLabel.Text = $"HP: {HealthComponent.Value}";
            }
        }
    }

    public CombatantData() : base() { _cache = new(this); }

    public void OnHealthChanged(int value)
    {
        void UpdateHealth(double hp)
        {
            HealthBar.Value = hp;
            HealthLabel.Text = $"HP: {(int)hp}";
        }

        if (!Engine.IsEditorHint() && IsInsideTree())
            CreateTween().TweenMethod(Callable.From<double>(UpdateHealth), HealthBar.Value, value, TransitionDuration);
        else
            UpdateHealth(value);
    }
}