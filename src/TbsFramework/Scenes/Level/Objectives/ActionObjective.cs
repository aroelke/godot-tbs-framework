using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Actions;
using TbsFramework.Scenes.Rendering;

namespace TbsFramework.Scenes.Level.Objectives;

/// <summary>Objective that's completed upon performing a special action in a region.</summary>
[Tool]
public partial class ActionObjective : Objective
{
    private SpecialActionRegionData _region = null;

    /// <summary>Region to perform the action in.  Also defines which units can perform the action. Side effects are not implemented here.</summary>
    [Export] public SpecialActionRegion ActionRegion = null;

    /// <summary>Action defining who can activate the region and what happens when the region is activated.</summary>
    [Export] public RegionUnitAction Action = null;

    /// <summary>
    /// If the region is:
    /// <list type="bullet">
    ///   <item>A once-per-cell region, represents the number of spaces the action must be performed in, with 0 representing "all of them"</item>
    ///   <item>A once-per-unit region, represents the number of different units that must perform the action, with 0 representing "all of them"</item>
    ///   <item>Neither, represents the number of times the action must be performed, with 0 being invalid</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Note that, if 0 is set for a one-shot region whose allowed units include all those from any factions, the set of allowed units updates as
    /// units in those factions enter and leave the map.
    /// </remarks>
    [Export(PropertyHint.Range, "0,10,or_greater")] public int Target = 1;

    public override bool Complete
    {
        get
        {
            if (Action is null || _region is null)
                return false;
            else if (Action.OncePerCell)
            {
                if (Target == 0)
                    return _region.Cells.Count == 0;
                else // if each cell can only be used once, then the number of times activated is the same as the number of cells activated
                    return _region.Performed.Values.Sum() >= Target;
            }
            else if (Action.OncePerUnit)
            {
                if (Target == 0)
                    return Action.AllAllowedUnits(_region.Grid).SetEquals(_region.Performed.Keys);
                else
                    return _region.Performed.Count >= Target;
            }
            else
                return Target > 0 && _region.Performed.Values.Sum() >= Target;
        }
    }

    public override string Description
    {
        get
        {
            if (Action is null)
                return "";
            else if (Action.OncePerCell)
                return $"{Action.Name} in {(Target == 0 ? "all" : Target)} space(s)";
            else if (Action.OncePerUnit)
                return $"{Action.Name} with {(Target == 0 ? "all" : Target)} allowed unit(s)";
            else
                return $"{Action.Name} {Target} time(s)";
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];

        if (ActionRegion is null)
            warnings.Add("");

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint() && ActionRegion is not null)
            _region = ActionRegion.Data;
    }
}