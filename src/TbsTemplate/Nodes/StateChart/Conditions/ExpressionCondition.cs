using System;
using System.Linq;
using Godot;

namespace TbsTemplate.Nodes.StateCharts.Conditions;

/// <summary><see cref="StateChart"/> action condition based on computing an expression (written in GDScript!) using <see cref="StateChart.GetVariable"/>.</summary>
[GlobalClass, Tool]
public partial class ExpressionCondition : StateCondition
{
    /// <summary>Expression to evaluate.</summary>
    [Export(PropertyHint.Expression)] public string Expression = "";

    public override bool IsSatisfied(ChartNode source)
    {
        Expression expression = new();
        string[] properties = [.. source.StateChart.GetVariables().Select(static (s) => s.ToString())];
        if (expression.Parse(Expression, properties) != Error.Ok)
            throw new Exception($"Expression parse error: {expression.GetErrorText()} for expression \"{Expression}\"");

        Variant result = expression.Execute([.. properties.Select((s) => source.StateChart.GetVariable(s))]);
        if (expression.HasExecuteFailed())
            throw new Exception($"Expression execute error: {expression.GetErrorText()} for expression \"{Expression}\"");

        return result.AsBool();
    }
}