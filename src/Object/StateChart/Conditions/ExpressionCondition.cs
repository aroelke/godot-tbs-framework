using System;
using System.Linq;
using Godot;
using Object.StateChart.States;

namespace Object.StateChart.Conditions;

/// <summary><see cref="Transition"/> condition based on computing an expression (written in GDScript!) using <see cref="Chart.ExpressionProperties"/>.</summary>
[GlobalClass, Icon("res://icons/statechart/ExpressionCondition.svg"), Tool]
public partial class ExpressionCondition : Condition
{
    /// <summary>Expression to evaluate.</summary>
    [Export(PropertyHint.Expression)] public string Expression = "";

    public override bool IsSatisfied(Transition transition, State from)
    {
        Node node = from;
        while (IsInstanceValid(node) && node is not Chart)
            node = node.GetParent();
        Chart chart = node as Chart;
        if (!IsInstanceValid(chart))
            throw new ArgumentException("Could not find state chart node.");
        
        Expression expression = new();
        string[] properties = chart.ExpressionProperties.Keys.Select((s) => s.ToString()).ToArray();
        if (expression.Parse(Expression, properties) != Error.Ok)
            throw new Exception($"Expression parse error: {expression.GetErrorText()} for expression \"{Expression}\"");

        Variant result = expression.Execute(new(properties.Select((s) => chart.ExpressionProperties[s])));
        if (expression.HasExecuteFailed())
            throw new Exception($"Expression execute error: {expression.GetErrorText()} for expression \"{Expression}\"");

        return result.AsBool();
    }
}