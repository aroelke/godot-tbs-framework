using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Nodes.StateChart;

/// <summary>A component of a <see cref="Chart"/>.</summary>
[GlobalClass, Icon("res://icons/statechart/ChartNode.svg"), Tool]
public partial class ChartNode : Node
{
    private Chart _chart = null;

    private Chart GetChart() => GetParent() switch
    {
        Chart chart => chart,
        ChartNode node => node.GetChart(),
        _ => null
    };

    /// <summary>Chart containing the component.</summary>
    public Chart StateChart => _chart ??= GetChart();

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (StateChart is null)
            warnings.Add($"Chart nodes need to be part of a state chart.");

        return [.. warnings];
    }
}