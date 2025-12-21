using System.Collections.Generic;
using Godot;

namespace TbsTemplate.Nodes.StateCharts;

/// <summary>A component of a <see cref="StateChart"/>.</summary>
public partial class ChartNode : Node
{
    private StateChart _chart = null;

    private StateChart GetChart() => GetParent() switch
    {
        StateChart chart => chart,
        ChartNode node => node.GetChart(),
        _ => null
    };

    /// <summary>Chart containing the component.</summary>
    public StateChart StateChart => _chart ??= GetChart();

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (StateChart is null)
            warnings.Add($"Chart nodes need to be part of a state chart.");

        return [.. warnings];
    }
}