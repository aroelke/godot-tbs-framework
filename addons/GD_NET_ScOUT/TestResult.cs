using System;
using System.Collections.Generic;

using Godot;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GD_NET_ScOUT;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
public partial class TestResult : Control
{
    public const string ScenePath = "res://addons/GD_NET_ScOUT/test_result.tscn";
    private const string Prefix = "[b][font_size=24][center]";
    private const string Suffix = "[/center][/font_size][/b]";

    [Export] private RichTextLabel _typeNameLabel;
    [Export] private VBoxContainer _methodList;

    public Result Result { get; set; } = Result.Success;
    public string TypeName { get; private set; } = string.Empty;
    public List<MethodResult> MethodResults { get; } = new();

    public override void _EnterTree()
    {
        base._EnterTree();
        ResetMethodList();
        MethodResults.Clear();
    }

    public void SetTypeName(string typeName)
    {
        TypeName = typeName;
        _typeNameLabel.Text = $"{Prefix}{TypeName}{Suffix}";
    }

    public void UpdateResult()
    {
        if (Result.Value != Result.Type.Success)
        {
            ResetMethodList();
            _typeNameLabel.Text = string.IsNullOrEmpty(Result.Message)
                ? $"{Prefix}{TypeName}\n{Result.Value}{Suffix}"
                : $"{Prefix}{TypeName}\n{Result.Value}: {Result.Message}{Suffix}";
            _typeNameLabel.Modulate =
                Result.Value == Result.Type.Skipped ? Colors.Yellow : Colors.Red;
            return;
        }

        int successCount = 0;
        int failCount = 0;
        int errorCount = 0;
        int skipCount = 0;
        foreach (var methodResult in MethodResults)
        {
            methodResult.Update();
            switch (methodResult.Function.Result.Value)
            {
                case Result.Type.Success:
                    successCount++;
                    break;
                case Result.Type.Failure:
                    failCount++;
                    break;
                case Result.Type.Error:
                    errorCount++;
                    break;
                case Result.Type.Skipped:
                    skipCount++;
                    break;
            }
        }
        _typeNameLabel.Text =
            $"{Prefix}{TypeName}\nTests: {MethodResults.Count}, Failures: {failCount}, Errors: {errorCount}, Skipped: {skipCount}{Suffix}";
        if (errorCount != 0)
        {
            _typeNameLabel.Modulate = Colors.Red;
        }
        else if (failCount == 0 && successCount > 0)
        {
            _typeNameLabel.Modulate = Colors.Green;
        }
        else if (successCount == 0 && failCount > 0)
        {
            _typeNameLabel.Modulate = Colors.Red;
        }
        else
        {
            _typeNameLabel.Modulate = Colors.Yellow;
        }
    }

    public void AddMethodResult(TestFunction function)
    {
        var methodResult = GD.Load<PackedScene>(MethodResult.ScenePath).Instantiate<MethodResult>();
        methodResult.SetMethodResult(function);
        _methodList.AddChild(methodResult);
        MethodResults.Add(methodResult);
    }

    private void ResetMethodList()
    {
        foreach (Node child in _methodList.GetChildren())
        {
            child.QueueFree();
        }
    }
}
