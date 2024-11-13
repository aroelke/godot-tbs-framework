using Godot;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that's completed upon performing a special action in a region.</summary>
[Tool]
public partial class ActionObjective : Objective
{
    private int _completed = 0;

    /// <summary>Region to perform the action in.  Also defines which units can perform the action. Side effects are not implemented here.</summary>
    [Export] public SpecialActionRegion ActionRegion = null;

    /// <summary>
    /// If the region is a one-shot region, represents the number of spaces in it that need to be acted upon, with 0 representing "all of them." If it
    /// isn't, then represents the number of units that need to perform the action, with 0 representing "all of them."
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
            }
            else
            {
                if (Target == 0)
                    return _completed >= ActionRegion.AllAllowedUnits().Count;
            }
            return _completed >= Target;
        }
    }

    public override string Description
    {
        get
        {
            if (ActionRegion is null)
                return "";
            else if (ActionRegion.OneShot)
                return $"{ActionRegion.Action} in {(Target == 0 ? "all" : Target)} spaces of {ActionRegion.Name}";
            else
                return $"{ActionRegion.Action} with {(Target == 0 ? "all" : Target)} allowed units";
        }
    }

    public void ActionPerformed(StringName action, Unit actor, Vector2I cell) => _completed++;

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint() && ActionRegion is not null)
            ActionRegion.SpecialActionPerformed += ActionPerformed;
    }
}