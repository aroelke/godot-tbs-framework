using Godot;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

namespace GD_NET_ScOUT;

public partial class MethodResult : Control
{
    public const string ScenePath = "res://addons/GD_NET_ScOUT/method_result.tscn";

    [Export] private RichTextLabel _methodName;
    [Export] private RichTextLabel _result;

    public TestFunction Function { get; private set; }

    public void SetMethodResult(TestFunction function)
    {
        Function = function;
        _methodName.Text = function.Name;
        Reset();
    }

    public void Update()
    {
        _result.Text = $"{Function.Result} {Function.TimeMilliseconds / 1000f}s";
        Modulate = Function.Result.Value switch
        {
            Result.Type.Success => Colors.Green,
            Result.Type.Failure => Colors.Red,
            Result.Type.Error => Colors.Red,
            Result.Type.Skipped => Colors.Yellow
        };
    }

    public void Reset()
    {
        _result.Text = string.Empty;
        SelfModulate = Colors.White;
    }
}
