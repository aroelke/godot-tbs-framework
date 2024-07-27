using System;
using System.Collections.Generic;
using Godot;

namespace Nodes.StateChart;

/// <summary>A component of a <see cref="Chart"/>.</summary>
[GlobalClass, Icon("res://icons/statechart/ChartNode.svg"), Tool]
public partial class ChartNode : Node
{
    private Chart GetChart() => GetParent() switch
    {
        Chart chart => chart,
        ChartNode node => node.GetChart(),
        _ => null
    };

    /// <summary>Chart containing the component.</summary>
    public Chart StateChart { get; private set; } = null;

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            StateChart = GetChart();
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        if (GetChart() is null)
            warnings.Add($"Chart nodes need to be part of a state chart.");

        return [.. warnings];
    }
}