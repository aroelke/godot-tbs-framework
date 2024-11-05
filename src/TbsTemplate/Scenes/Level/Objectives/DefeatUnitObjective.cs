using System.Collections.Generic;
using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that's accomplished when a specific unit is defeated. Reassigning the target unit will uncomplete the objective.</summary>
[Tool]
public partial class DefeatUnitObjective : Objective
{
    private Unit _target = null;

    /// <summary>Unit to defeat to accomplish the objective.</summary>
    [Export] public Unit Target
    {
        get => _target;
        set
        {
            if (Engine.IsEditorHint())
                _target = value;
            else
            {
                if (!IsInstanceValid(value))
                {
                    GD.PrintErr($"Cannot assign an invalid unit as a target for {Name}.");
                    value = null;
                }
                if (_target != value)
                {
                    _target = value;
                    Completed = false;
                }
            }
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (Target is null)
            warnings.Add("A target needs to be set for this objective to be completable.");
        
        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            UnitEvents.Singleton.UnitDefeated += (u) => {
                if (u == Target)
                {
                    Completed = true;
                    _target = null;
                }
            };
        }
    }
}