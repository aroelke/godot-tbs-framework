using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Scenes.Level.Layers;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Scenes.Level.Objectives;

/// <summary>Objective that's completed upon performing a special action in a region.</summary>
[Tool]
public partial class ActionObjective : Objective
{
    private SpecialActionRegionData _region = null;
    private readonly List<UnitData> _units = [];
    private readonly List<Vector2I> _spaces = [];

    /// <summary>Region to perform the action in.  Also defines which units can perform the action. Side effects are not implemented here.</summary>
    [Export] public SpecialActionRegion ActionRegion = null;

    /// <summary>
    /// If the region is:
    /// <list type="bullet">
    ///   <item>A one-shot region, represents the number of spaces the action must be performed in, with 0 representing "all of them"</item>
    ///   <item>A single-use region, represents the number of different units that must perform the action, with 0 representing "all of them"</item>
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
            if (_region is null)
                return false;
            else if (_region.OneShot)
            {
                if (Target == 0)
                    return _region.Cells.Count == 0;
                else
                    return _spaces.Count >= Target;
            }
            else if (_region.SingleUse)
            {
                if (Target == 0)
                    return !_region.AllAllowedUnits().Any();
                else
                    return _region.Performed.Count >= Target;
            }
            else
                return Target > 0 && _units.Count >= Target;
        }
    }

    public override string Description
    {
        get
        {
            if (_region is null)
                return "";
            else if (_region.OneShot)
                return $"{_region.Action} in {(Target == 0 ? "all" : Target)} space(s) of {_region.Action}";
            else if (_region.SingleUse)
                return $"{_region.Action} with {(Target == 0 ? "all" : Target)} allowed unit(s)";
            else
                return $"{_region.Action} {Target} time(s)";
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
        {
            _region = ActionRegion.Data;
            _region.ActionPerformed += (_, unit, cell) => {
                _units.Add(unit);
                _spaces.Add(cell);
            };
        }
    }
}