using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Scenes.Level.Control;

[Tool]
public partial class ManualSwitchCondition : SwitchCondition
{
    public void Trigger() => Satisfied = !Satisfied;

    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        base._ValidateProperty(property);
        if (property["name"].AsStringName() == PropertyName.TriggerUnits || property["name"].AsStringName() == PropertyName.TriggerArmies)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = [.. base._GetConfigurationWarnings() ?? []];
        warnings.Remove(NoApplicableUnitsWarning);
        return [.. warnings];
    }
}