using System;
using System.Linq;
using Godot;
using TbsTemplate.Nodes.Components;
using TbsTemplate.Scenes.Level.State.Components;

namespace TbsTemplate.UI.Combat;

/// <summary>Combat UI element that displays constant information about a character participating in combat.</summary>
[SceneTree, Tool]
public partial class ParticipantInfo : GridContainer
{
    private HealthState _health = new();
    private int[] _damage = [0];
    private int _hit = 0;

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

    public HealthState Health
    {
        get => _health;
        set
        {
            if (value is not null)
            {
                _health.Maximum = value.Maximum;
                _health.Value = value.Value;

                if (HealthBar is not null)
                {
                    HealthBar.MaxValue = _health.Maximum;
                    HealthBar.Value = _health.Value;
                }
                if (HealthLabel is not null)
                    HealthLabel.Text = $"HP: {_health.Value}";
            }
        }
    }

    public void OnHealthChanged(double value)
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

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
            _health.ValueChanged += OnHealthChanged;
    }
}