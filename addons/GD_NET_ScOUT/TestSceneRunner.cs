using System;

using Godot;

namespace GD_NET_ScOUT;

/// <summary>
/// <para>A class for setting the <see cref="T:GD_NET_ScOUT.SceneScout"/> singleton node's parameters and initiating test execution.</para>
/// <para><see cref="T:GD_NET_ScOUT.TestSceneRunner"/> scenes should be configured and created in the ScOUT editor tab.</para>
/// </summary>
[Tool]
public partial class TestSceneRunner : Node
{
    public const string DefaultTestSceneDir = "res://test/scenes";
    public const string ScenePath = "res://addons/GD_NET_ScOUT/test_scene_runner.tscn";

    [Export(PropertyHint.ResourceType, nameof(PackedScene))]
    public PackedScene[]? Scenes;

    [Export(PropertyHint.Dir)] public string[]? TestSceneDirectories = {DefaultTestSceneDir};

    [Export] public bool Verbose = true;
    [Export] public bool PrintReportToStdOut = true;
    [Export] public bool SaveReportToFile = true;

    [Export(PropertyHint.GlobalSaveFile, "*.xml")]
    public string TestReportPath = string.Empty;

    public override void _Ready()
    {
        try
        {
            this.GetSceneScout()
                ?.Init(
                    Verbose, Scenes, TestSceneDirectories, PrintReportToStdOut, SaveReportToFile,
                    TestReportPath
                );
        }
        catch (Exception)
        {
            GetTree().Quit((int)Error.Failed);
            throw;
        }
    }
}
