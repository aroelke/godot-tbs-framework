using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Objectives;

/// <summary>Objective that's completed upon performing a special action in a region.</summary>
[Tool]
public partial class ActionObjective : Objective
{
    private readonly List<Unit> _units = [];
    private readonly List<Vector2I> _spaces = [];

    /// <summary>Region to perform the action in.  Also defines which units can perform the action. Side effects are not implemented here.</summary>
    [Export] public SpecialActionRegion ActionRegion = null;

    /// <summary>
    /// If the region is:
    /// - A one-shot region, represents the number of spaces the action must be performed in, with 0 representing "all of them"
    /// - A single-use region, represents the number of different units that must perform the action, with 0 representing "all of them"
    /// - Neither, represents the number of times the action must be performed, with 0 being invalid
    /// </summary>
    [Export(PropertyHint.Range, "0,10,or_greater")] public int Target = 1;

    public override bool Complete
    {
        get
        {
            if (ActionRegion is null)
                return false;
            else if (ActionRegion.OneShot)
            {
                if (Target == 0)
                    return ActionRegion.GetUsedCells().Count == 0;
                else
                    return _spaces.Count >= Target;
            }
            else if (ActionRegion.SingleUse)
            {
                if (Target == 0)
                    return ActionRegion.Performed == ActionRegion.AllAllowedUnits();
                else
                    return ActionRegion.Performed.Count >= Target;
            }
            else
                return Target > 0 && _units.Count >= Target;
        }
    }

    public override string Description
    {
        get
        {
            if (ActionRegion is null)
                return "";
            else if (ActionRegion.OneShot)
                return $"{ActionRegion.Action} in {(Target == 0 ? "all" : Target)} space(s) of {ActionRegion.Name}";
            else if (ActionRegion.SingleUse)
                return $"{ActionRegion.Action} with {(Target == 0 ? "all" : Target)} allowed unit(s)";
            else
                return $"{ActionRegion.Action} {Target} time(s)";
        }
    }

    public void ActionPerformed(StringName action, Unit actor, Vector2I cell)
    {
        if (action == ActionRegion.Action)
        {
            _units.Add(actor);
            _spaces.Add(cell);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint() && ActionRegion is not null)
            ActionRegion.SpecialActionPerformed += ActionPerformed;
    }
}