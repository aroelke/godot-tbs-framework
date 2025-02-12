using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using Godot;

namespace GD_NET_ScOUT;

/// <summary>
/// <para>This class should only be used internally by <see cref="T:GD_NET_ScOUT.TestSceneRunner"/> and <see cref="T:GD_NET_ScOUT.TestRunner"/>.</para>
/// </summary>
internal partial class SceneScout : Node
{
    public const string AutoloadName = "SceneScout";
    public const double MaxTestRunnerMissingWaitSeconds = 1.0;
    private const string TextSceneFileExtension = ".tscn";
    private const string XmlVersionEncoding = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

    internal bool Active { get; private set; }
    internal bool SingleScene { get; private set; }
    internal bool Verbose { get; private set; }
    internal TestRunner? CurrentTestRunner { get; private set; }
    private IEnumerator<PackedScene> _testScenes = Enumerable.Empty<PackedScene>().GetEnumerator();
    private readonly Dictionary<string, TestResult[]> _sceneNameToResults = new();
    private bool _printReportToStdOut;
    private bool _saveReportToFile;
    private string _testReportPath = string.Empty;
    private double _testRunnerMissingWaitSeconds;

    public void Init(
        bool verbose, IEnumerable<PackedScene>? scenes, string[]? paths, bool printReportToStdOut,
        bool saveReportToFile, string testReportPath)
    {
        if (Active)
        {
            throw new ApplicationException("SceneScout already initialized");
        }

        Active = true;
        SingleScene = false;
        Verbose = verbose;
        _testScenes = LoadTestScenes(scenes, paths).GetEnumerator();
        _sceneNameToResults.Clear();
        _printReportToStdOut = printReportToStdOut;
        _saveReportToFile = saveReportToFile;
        _testReportPath = testReportPath.EndsWith(".xml")
            ? testReportPath
            : $"{testReportPath}.xml";
        EnsureValidTestReportPath();
        CallDeferred(MethodName.RunNextTestScene);
    }

    public void RegisterTestRunner(TestRunner testRunner)
    {
        if (CurrentTestRunner is not null)
        {
            GD.PrintErr(
                $"There must only be one test runner per test scene; unloading {CurrentTestRunner.Name}"
            );
            CurrentTestRunner.GetParent()
                .CallDeferred(Node.MethodName.RemoveChild, CurrentTestRunner);
            CurrentTestRunner = testRunner;
        }

        CurrentTestRunner = testRunner;
        if (Active)
        {
            return;
        }

        Active = true;
        SingleScene = true;
    }

    public void ReportTestSceneComplete(IEnumerable<TestResult> results)
    {
        if (!Active || SingleScene)
        {
            return;
        }

        _sceneNameToResults[_testScenes.Current.ResourcePath] = results.ToArray();
        RunNextTestScene();
    }

    public override void _Process(double delta)
    {
        if (!Active
            || SingleScene
            || CurrentTestRunner is not null
            || (_testRunnerMissingWaitSeconds += delta) < MaxTestRunnerMissingWaitSeconds)
        {
            return;
        }

        CallDeferred(MethodName.RunNextTestScene);
        GD.PrintErr(
            $"{_testScenes.Current.ResourcePath}\nNo test runner found in scene. Skipping..."
        );
    }

    private IEnumerable<PackedScene> LoadTestScenes(
        IEnumerable<PackedScene>? scenes, string[]? paths)
    {
        ISet<PackedScene> testScenes = scenes?.ToHashSet() ?? new HashSet<PackedScene>();
        if (paths?.Any() != true)
        {
            return testScenes;
        }

        foreach (string rawPath in paths)
        {
            string path = rawPath.EndsWith('/') ? rawPath : $"{rawPath}/";
            using DirAccess dir = DirAccess.Open(path);
            if (dir is null)
            {
                throw new ApplicationException(
                    $"Error attempting to open directory {path}: {DirAccess.GetOpenError()}"
                );
            }
            dir.ListDirBegin();
            string fileName;
            while (!string.IsNullOrEmpty(fileName = dir.GetNext().TrimSuffix(".remap")))
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(TextSceneFileExtension))
                {
                    testScenes.Add(GD.Load<PackedScene>($"{path}{fileName}"));
                }
            }
        }
        return testScenes;
    }

    private void EnsureValidTestReportPath()
    {
        if (!_saveReportToFile || EnsureDirsExist() == Error.Ok)
        {
            return;
        }

        throw new ApplicationException(
            $"Error attempting to create open/create directory "
            + $"{_testReportPath[.._testReportPath.LastIndexOf('/')]}: {DirAccess.GetOpenError()}"
        );
    }

    private Error EnsureDirsExist()
    {
        if (!_testReportPath.StartsWith("res://") && !_testReportPath.StartsWith("user://"))
        {
            return DirAccess.MakeDirRecursiveAbsolute(
                _testReportPath[.._testReportPath.LastIndexOf('/')]
            );
        }

        int startDirIndex = _testReportPath.GodotPathLen();
        int endDirIndex = _testReportPath.LastIndexOf('/');
        if (endDirIndex <= startDirIndex)
        {
            return Error.Ok;
        }

        using var dir = DirAccess.Open(_testReportPath[..startDirIndex]);
        return dir.MakeDirRecursive(_testReportPath[startDirIndex..endDirIndex]);
    }

    private void RunNextTestScene()
    {
        if (_testScenes.MoveNext())
        {
            CallDeferred(MethodName.ChangeToNextTestScene);
            return;
        }

        Active = false;
        string? xml = null;
        if (_saveReportToFile)
        {
            using var file = FileAccess.Open(_testReportPath, FileAccess.ModeFlags.Write);
            xml ??= TestResultXmlBuilder.BuildFullXml(_sceneNameToResults).ToString();
            file.StoreString(XmlVersionEncoding);
            file.StoreString("\n");
            file.StoreString(xml);
            file.StoreString("\n");
            file.Close();
            GD.Print(
                Verbose
                    ? $"\nTest report saved to:\n{file.GetPathAbsolute()}"
                    : file.GetPathAbsolute()
            );
        }
        if (_printReportToStdOut)
        {
            xml ??= TestResultXmlBuilder.BuildFullXml(_sceneNameToResults).ToString();
            GD.Print(XmlVersionEncoding);
            GD.Print(xml);
        }

        Result.Type worstResult = _sceneNameToResults.Values
            .SelectMany(results => results.AsEnumerable())
            .SelectMany(result => result.MethodResults)
            .Select(result => result.Function.Result.Value)
            .DefaultIfEmpty(Result.Type.Success)
            .Max();
        GetTree().Quit((int)(worstResult <= Result.Type.Skipped ? Error.Ok : Error.Failed));
    }

    private void ChangeToNextTestScene()
    {
        CurrentTestRunner = null;
        _testRunnerMissingWaitSeconds = 0.0;
        GetTree().ChangeSceneToPacked(_testScenes.Current);
    }
}

// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
internal static class TestResultXmlBuilder
{
    public static XElement BuildFullXml(Dictionary<string, TestResult[]> sceneNameToResults)
    {
        int testsCount = 0;
        int failuresCount = 0;
        int errorsCount = 0;
        int skippedCount = 0;
        long timeMilliseconds = 0L;
        object[] sceneXmls = sceneNameToResults.SelectMany(
                sceneNameToTestClasses =>
                {
                    string sceneName = sceneNameToTestClasses.Key[
                            sceneNameToTestClasses.Key.GodotPathLen()..sceneNameToTestClasses.Key
                                .LastIndexOf('.')]
                        .Replace('/', '.');
                    return sceneNameToTestClasses.Value.Select(
                        result => BuildClassXml(
                            sceneName, result, ref testsCount, ref failuresCount, ref errorsCount,
                            ref skippedCount, ref timeMilliseconds
                        )
                    );
                }
            )
            .Select(o => (object)o)
            .ToArray();
        XElement testSuites = new XElement(
            "testsuites", new XAttribute("name", "GD_NET_ScOUT"),
            new XAttribute("tests", testsCount), new XAttribute("failures", failuresCount),
            new XAttribute("errors", errorsCount), new XAttribute("skipped", skippedCount),
            new XAttribute("time", timeMilliseconds / 1000f)
        );
        testSuites.Add(sceneXmls);
        return testSuites;
    }

    private static XElement BuildClassXml(
        string sceneName, TestResult testResult, ref int sceneTestsCount,
        ref int sceneFailuresCount, ref int sceneErrorsCount, ref int sceneSkippedCount,
        ref long sceneTimeMilliseconds)
    {
        int testsCount = 0;
        int failuresCount = 0;
        int errorsCount = 0;
        int skippedCount = 0;
        long timeMilliseconds = 0L;
        string className = $"{sceneName}.{testResult.TypeName}";
        object[] methodXmls = testResult.MethodResults.Select(result => result.Function)
            .Select(
                function => (object)BuildMethodXml(
                    testResult, className, function, ref testsCount, ref failuresCount,
                    ref errorsCount, ref skippedCount, ref timeMilliseconds
                )
            )
            .ToArray();
        sceneTestsCount += testsCount;
        sceneFailuresCount += failuresCount;
        sceneErrorsCount += errorsCount;
        sceneSkippedCount += skippedCount;
        sceneTimeMilliseconds += timeMilliseconds;
        XElement testSuite = new XElement(
            "testsuite", new XAttribute("name", className), new XAttribute("tests", testsCount),
            new XAttribute("failures", failuresCount), new XAttribute("errors", errorsCount),
            new XAttribute("skipped", skippedCount),
            new XAttribute("time", timeMilliseconds / 1000f)
        );
        testSuite.Add(methodXmls);
        return testSuite;
    }

    private static XElement BuildMethodXml(
        TestResult classTestResult, string className, TestFunction testFunction,
        ref int classTestsCount, ref int classFailuresCount, ref int classErrorsCount,
        ref int classSkippedCount, ref long classTimeMilliseconds)
    {
        classTestsCount++;
        classTimeMilliseconds += testFunction.TimeMilliseconds;
        XElement testcase = new XElement(
            "testcase", new XAttribute("name", testFunction.Name),
            new XAttribute("classname", className),
            new XAttribute("time", testFunction.TimeMilliseconds / 1000f)
        );
        testcase.Add(
            BuildMethodXmlHelper(
                classTestResult.Result, true, ref classFailuresCount, ref classErrorsCount,
                ref classSkippedCount
            )
        );
        testcase.Add(
            BuildMethodXmlHelper(
                testFunction.Result, false, ref classFailuresCount, ref classErrorsCount,
                ref classSkippedCount
            )
        );
        return testcase;
    }

    private static XElement? BuildMethodXmlHelper(
        Result result, bool isClassResult, ref int classFailuresCount, ref int classErrorsCount,
        ref int classSkippedCount)
    {
        switch (result.Value)
        {
            case Result.Type.Success:
                return null;
            case Result.Type.Failure:
                classFailuresCount++;
                break;
            case Result.Type.Error:
                classErrorsCount++;
                break;
            case Result.Type.Skipped:
                classSkippedCount++;
                break;
        }

        string value = result.Value.ToString().ToLower(CultureInfo.InvariantCulture);
        string message = isClassResult ? $"Class {value}: {result.Message}" : result.Message;
        return new XElement(value, new XAttribute("message", message));
    }
}
