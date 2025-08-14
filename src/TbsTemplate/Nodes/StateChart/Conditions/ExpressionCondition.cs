using System;
using System.Linq;
using Godot;

namespace TbsTemplate.Nodes.StateChart.Conditions;

/// <summary><see cref="Chart"/> action condition based on computing an expression (written in GDScript!) using <see cref="Chart.GetExpressionProperty"/>.</summary>
[GlobalClass, Icon("res://icons/statechart/ExpressionCondition.svg"), Tool]
public partial class ExpressionCondition : Condition
{
    /// <summary>Expression to evaluate.</summary>
    [Export(PropertyHint.Expression)] public string Expression = "";

    public override bool IsSatisfied(ChartNode source)
    {
        Expression expression = new();
        string[] properties = [.. source.StateChart.GetExpressionProperties().Select(static (s) => s.ToString())];
        if (expression.Parse(Expression, properties) != Error.Ok)
            throw new Exception($"Expression parse error: {expression.GetErrorText()} for expression \"{Expression}\"");

        Variant result = expression.Execute([.. properties.Select((s) => source.StateChart.GetExpressionProperty(s))]);
        if (expression.HasExecuteFailed())
            throw new Exception($"Expression execute error: {expression.GetErrorText()} for expression \"{Expression}\"");

        return result.AsBool();
    }
}