using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Godot;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GD_NET_ScOUT;

/// <summary>
/// <para>The main class that runs scene tests. Every test scene must have one <see cref="T:GD_NET_ScOUT.TestRunner"/> node, typically <code>res://addons/GD_NET_ScOUT/test_runner.tscn</code>.</para>
/// <para>Test classes can access the active <see cref="T:GD_NET_ScOUT.TestRunner"/> using the extension method <see cref="M:GD_NET_ScOUT.Extensions.GetTestRunner(Godot.Node)"/>.</para>
/// <para>This class has several useful methods for helping run tests. See: <list>
/// <item><see cref="M:GD_NET_ScOUT.TestRunner.Print(System.String,System.Object[])"/></item>
/// <item><see cref="M:GD_NET_ScOUT.TestRunner.PrintRich(System.String,System.Object[])"/></item>
/// <item><see cref="M:GD_NET_ScOUT.TestRunner.PrintErr(System.String,System.Object[])"/></item>
/// <item><see cref="M:GD_NET_ScOUT.TestRunner.WaitFrames(System.Int32,System.Action)"/></item>
/// <item><see cref="M:GD_NET_ScOUT.TestRunner.WaitSeconds(System.Double,System.Action)"/></item>
/// <item><see cref="M:GD_NET_ScOUT.TestRunner.WaitForSignal(Godot.Node,Godot.StringName,System.Action,System.Double)"/></item>
/// </list></para>
/// </summary>
/// <seealso cref="T:GD_NET_ScOUT.TestRunnerTest"/>
public partial class TestRunner : Control
{
    [Export] private bool _runTestsOnSceneLoad = true;
    [Export] private bool _verbose = true;
    [Export] private ShowHideButton _showHideButton;
    [Export] private Button _loadTests;
    [Export] private Button _runTests;
    [Export] private VBoxContainer _testList;

    private long Wait
    {
        get => Interlocked.Read(ref _wait);
        set => Interlocked.Exchange(ref _wait, value);
    }

    private bool IsOwnedBySceneScout => this.GetSceneScout()?.SingleScene == false;
    private bool Verbose => IsOwnedBySceneScout ? this.GetSceneScout()!.Verbose : _verbose;

    private readonly Dictionary<TestClass, TestResult> _testClassToResult = new();
    private readonly Dictionary<TestClass, TestFunction[]> _testClassToFunctions = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly List<(int, Action?)> _toWaitFrames = new();
    private readonly List<Action> _invokeOnException = new();
    private long _wait;
    private bool _testsRunning;

    private IEnumerator<TestClass> _testClasses = Enumerable.Empty<TestClass>().GetEnumerator();

    private IEnumerator<TestFunction> _testFunctions =
        Enumerable.Empty<TestFunction>().GetEnumerator();

    private TestStage _stage;
    private long _elapsedMilliseconds;

    /// <summary>
    /// <para>Utility method for printing to the console that respects <see cref="P:GD_NET_ScOUT.TestRunner.Verbose" />.</para>
    /// <para>See <see cref="M:Godot.GD.Print(System.String)" /> and <see cref="M:System.String.Format(System.String,System.Object[])" />.</para>
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public void Print(string format, params object?[] args)
    {
        if (!Verbose)
        {
            return;
        }

        GD.Print(string.Format(format, args));
    }

    /// <summary>
    /// <para>Utility method for printing to the console that respects <see cref="P:GD_NET_ScOUT.TestRunner.Verbose" />.</para>
    /// <para>See <see cref="M:Godot.GD.PrintRich(System.String)" /> and <see cref="M:System.String.Format(System.String,System.Object[])" />.</para>
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public void PrintRich(string format, params object?[] args)
    {
        if (!Verbose)
        {
            return;
        }

        GD.PrintRich(string.Format(format, args));
    }

    /// <summary>
    /// <para>Utility method for printing to the console that respects <see cref="P:GD_NET_ScOUT.TestRunner.Verbose" />.</para>
    /// <para>See <see cref="M:Godot.GD.PrintErr(System.String)" /> and <see cref="M:System.String.Format(System.String,System.Object[])" />.</para>
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public void PrintErr(string format, params object?[] args)
    {
        if (!Verbose)
        {
            return;
        }

        GD.PrintErr(string.Format(format, args));
    }

    /// <summary>
    /// <para>Equivalent to calling <see cref="M:GD_NET_ScOUT.TestRunner.WaitFrames(System.Int32,System.Action)">WaitFrames(1, action)</see>.</para>
    /// </summary>
    public void InvokeNextFrame(Action action) => WaitFrames(1, action);

    /// <summary>
    /// <para>Wait <paramref name="frames" /> frames, then invoke <paramref name="action" /> at the beginning of the next invocation of <see cref="M:Godot.Node._Process(System.Double)" />.</para>
    /// <para>Consider using <see cref="M:Godot.GodotObject.CallDeferred(Godot.StringName,Godot.Variant[])"/>. Use this method instead when actions need to be chained across multiple frames, or when emitting signals from within signal callbacks.</para>
    /// <para>Calls to <see cref="M:GD_NET_ScOUT.TestRunner.WaitFrames(System.Int32,System.Action)"/>, <see cref="M:GD_NET_ScOUT.TestRunner.WaitSeconds(System.Double,System.Action)"/> and <see cref="M:GD_NET_ScOUT.TestRunner.WaitForSignal(Godot.Node,Godot.StringName,System.Action,System.Double)"/> can be chained together by nesting calls within <paramref name="action" />.</para>
    /// </summary>
    /// <param name="frames">The number of frames to wait.</param>
    /// <param name="action">The action to invoke next frame.</param>
    /// <seealso cref="M:GD_NET_ScOUT.TestRunnerTest.WaitFrames">TestRunnerTest.WaitFrames()</seealso>
    public void WaitFrames(int frames, Action? action = null)
    {
        if (frames <= 0)
        {
            throw new ApplicationException(
                $"Cannot wait {frames} frames! Must be non-negative integer."
            );
        }

        _toWaitFrames.Add((frames, action));
    }

    /// <summary>
    /// <para>Make the <see cref="T:GD_NET_ScOUT.TestRunner"/> wait <paramref name="timeSeconds"/> seconds before moving on to the next test stage. If included, also invoke <paramref name="action" /> after <paramref name="timeSeconds"/>.</para>
    /// <para>NOTE: Does not pause execution; to pause execution, use an await statement.</para>
    /// <para>If the test fails or an exception is thrown, the callback will be safely canceled if it is still connected.</para>
    /// <para>Calls to <see cref="M:GD_NET_ScOUT.TestRunner.WaitFrames(System.Int32,System.Action)"/>, <see cref="M:GD_NET_ScOUT.TestRunner.WaitSeconds(System.Double,System.Action)"/> and <see cref="M:GD_NET_ScOUT.TestRunner.WaitForSignal(Godot.Node,Godot.StringName,System.Action,System.Double)"/> can be chained together by nesting calls within <paramref name="action" />.</para>
    /// </summary>
    /// <param name="timeSeconds">How many seconds to wait.</param>
    /// <param name="action">An optional action to perform after <paramref name="timeSeconds" /> seconds.</param>
    /// <seealso cref="M:GD_NET_ScOUT.TestRunnerTest.WaitSeconds">TestRunnerTest.WaitSeconds()</seealso>
    public void WaitSeconds(double timeSeconds, Action? action = null)
    {
        Wait++;
        SafeConnect(
            GetTree().CreateTimer(timeSeconds), SceneTreeTimer.SignalName.Timeout,
            Callable.From(OnTimeout)
        );
        return;

        void OnTimeout()
        {
            try
            {
                action?.Invoke();
                Wait--;
            }
            catch (Exception e)
            {
                HandleTestException(e);
            }
        }
    }

    /// <summary>
    /// <para>Make the <see cref="T:GD_NET_ScOUT.TestRunner"/> wait until <paramref name="node"/> emits the signal <paramref name="signalName"/> before moving on to the next test stage. If included, also invoke <paramref name="action" /> after the signal is emitted. Times out if signal is not emitted after <paramref name="timeOutSeconds"/> seconds.</para>
    /// <para>NOTE: Does not pause execution; to pause execution, use an await statement.</para>
    /// <para>If the test fails or an exception is thrown, the callback will be safely canceled if it is still connected.</para>
    /// <para>Calls to <see cref="M:GD_NET_ScOUT.TestRunner.WaitFrames(System.Int32,System.Action)"/>, <see cref="M:GD_NET_ScOUT.TestRunner.WaitSeconds(System.Double,System.Action)"/> and <see cref="M:GD_NET_ScOUT.TestRunner.WaitForSignal(Godot.Node,Godot.StringName,System.Action,System.Double)"/> can be chained together by nesting calls within <paramref name="action" />.</para>
    /// </summary>
    /// <param name="node">The node that will emit the expected signal.</param>
    /// <param name="signalName">The name of the expected signal.</param>
    /// <param name="action">An optional action to take after the signal is emitted.</param>
    /// <param name="timeOutSeconds">How many seconds to wait for the signal before disconnecting the callback and failing the test.</param>
    /// <seealso cref="M:GD_NET_ScOUT.TestRunnerTest.WaitForSignal">TestRunnerTest.WaitForSignal()</seealso>
    public void WaitForSignal(
        Node node, StringName signalName, Action? action = null, double timeOutSeconds = 5.0)
    {
        Wait++;
        SceneTreeTimer timer = GetTree().CreateTimer(timeOutSeconds);
        Callable onTimeout = Callable.From(() => {});
        Callable onSignal = Callable.From(Action);
        onTimeout = Callable.From(Timeout);
        SafeConnect(timer, SceneTreeTimer.SignalName.Timeout, onTimeout);
        SafeConnect(node, signalName, onSignal, (uint)ConnectFlags.OneShot);
        return;

        void Action()
        {
            try
            {
                // ReSharper disable once AccessToModifiedClosure
                SafeDisconnect(timer, SceneTreeTimer.SignalName.Timeout, onTimeout);
                action?.Invoke();
                Wait--;
            }
            catch (Exception e)
            {
                HandleTestException(e);
            }
        }

        void Timeout()
        {
            try
            {
                SafeDisconnect(node, signalName, onSignal);
                Assert.Fail(
                    $"Timed out after {timeOutSeconds}s waiting for signal {signalName} on {node.Name}"
                );
            }
            catch (Exception e)
            {
                HandleTestException(e);
            }
        }
    }

    private void SafeConnect(
        GodotObject node, StringName signal, Callable callable, uint flags = 0u)
    {
        node.Connect(signal, callable, flags);
        _invokeOnException.Add(() => SafeDisconnect(node, signal, callable));
    }

    private void SafeDisconnect(GodotObject node, StringName signal, Callable callable)
    {
        if (node.IsConnected(signal, callable))
        {
            node.Disconnect(signal, callable);
        }
    }

    public override void _Ready()
    {
        this.GetSceneScout()?.RegisterTestRunner(this);
        Visible = !IsOwnedBySceneScout;
        OffsetRight = GetViewport().GetVisibleRect().Size.X / 2f;
        OffsetBottom = GetViewport().GetVisibleRect().Size.Y;
        LoadTests();
        if (_runTestsOnSceneLoad || IsOwnedBySceneScout)
        {
            WaitSeconds(0.25, RunTests);
        }
    }

    public override void _Process(double delta)
    {
        if (!_testsRunning)
        {
            return;
        }

        try
        {
            ProcessTestStage();
        }
        catch (Exception e)
        {
            if (!HandleTestException(e))
            {
                throw;
            }
        }
    }

    private void ProcessTestStage()
    {
        // NOTE: _toWaitFrames can be modified invoking its elements' actions
        if (_toWaitFrames.Any())
        {
            (int, Action?)[] toWaitFrames = _toWaitFrames.ToArray();
            _toWaitFrames.Clear();
            foreach ((int frames, Action? action) in toWaitFrames)
            {
                if (frames == 1)
                {
                    action?.Invoke();
                }
                else
                {
                    _toWaitFrames.Add((frames - 1, action));
                }
            }
        }
        if (Wait > 0L || _toWaitFrames.Any())
        {
            return;
        }

        _invokeOnException.Clear();
        switch (_stage)
        {
            case TestStage.NotStarted:
                if (!_testClasses.MoveNext())
                {
                    _testClasses.Dispose();
                    _stage = TestStage.Finished;
                    break;
                }

                Print(_testClasses.Current.Name);
                if (_testClasses.Current.Result.Value == Result.Type.Skipped)
                {
                    PrintRich(
                        "[color=yellow]  Skipped. {0}[/color]", _testClasses.Current.Result.Message
                    );
                    _testClassToResult[_testClasses.Current].Result = _testClasses.Current.Result;
                    break;
                }

                _testFunctions = _testClassToFunctions[_testClasses.Current]
                    .AsEnumerable()
                    .GetEnumerator();
                _stage = TestStage.BeforeAll;
                break;
            case TestStage.BeforeAll:
                _testClasses.Current.BeforeAll?.Invoke(_testClasses.Current.Instance, null);
                _stage = TestStage.BeforeEach;
                break;
            case TestStage.BeforeEach:
                if (!_testFunctions.MoveNext())
                {
                    _testFunctions.Dispose();
                    _stage = TestStage.AfterAll;
                    break;
                }

                Print("  {0}", _testFunctions.Current.Name);
                if (_testFunctions.Current.Result.Value == Result.Type.Skipped)
                {
                    PrintRich(
                        "[color=yellow]    Skipped. {0}[/color]",
                        _testFunctions.Current.Result.Message
                    );
                    break;
                }

                _elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
                _testClasses.Current.BeforeEach?.Invoke(_testClasses.Current.Instance, null);
                _stage = TestStage.Test;
                break;
            case TestStage.Test:
                _testFunctions.Current.Method.Invoke(_testClasses.Current.Instance, null);
                _stage = TestStage.AfterEach;
                break;
            case TestStage.AfterEach:
                _testClasses.Current.AfterEach?.Invoke(_testClasses.Current.Instance, null);
                _testFunctions.Current.TimeMilliseconds =
                    _stopwatch.ElapsedMilliseconds - _elapsedMilliseconds;
                Print(
                    "    {0} {1}s", _testFunctions.Current.Result,
                    _testFunctions.Current.TimeMilliseconds / 1000f
                );
                _stage = TestStage.BeforeEach;
                break;
            case TestStage.AfterAll:
                _testClasses.Current.AfterAll?.Invoke(_testClasses.Current.Instance, null);
                _stage = TestStage.NotStarted;
                break;
            case TestStage.Finished:
                _testsRunning = false;
                _stopwatch.Stop();
                Print("Tests ran in {0}s\n", _stopwatch.ElapsedMilliseconds / 1000f);
                Visible = !IsOwnedBySceneScout;
                foreach (TestResult result in _testClassToResult.Values)
                {
                    result.UpdateResult();
                }
                this.GetSceneScout()?.ReportTestSceneComplete(_testClassToResult.Values);
                break;
        }
    }

    /// <summary>
    /// <para>Handle an exception that was thrown during test invocation. Invoke all actions in <see cref="F:GD_NET_ScOUT.TestRunner._invokeOnException"/>.</para>
    /// <para>If <paramref name="exception"/> is assertion-based, mark the test class/method as a Failure. Otherwise, mark it as an Error.</para>
    /// <para>If <paramref name="exception"/> was thrown while invoking a [BeforeAll] or [AfterAll] method, mark the test class as a Failure/Error. If <paramref name="exception"/> was thrown while invoking a [Test], [BeforeEach] or [AfterEach] method, mark the test method as a Failure/Error.</para>
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>Whether <paramref name="exception"/> was handled. If false, consider re-throwing.</returns>
    private bool HandleTestException(Exception exception)
    {
        Exception e = exception is TargetInvocationException invocationException
            ? invocationException.InnerException ?? invocationException
            : exception;
        PrintErr(e.ToString());
        Result.Type resultType = e.GetType().Name.Contains("Assert")
            ? Result.Type.Failure
            : Result.Type.Error;

        foreach (Action action in _invokeOnException)
        {
            action.Invoke();
        }
        _invokeOnException.Clear();
        Wait = 0L;

        switch (_stage)
        {
            case TestStage.BeforeAll:
            case TestStage.AfterAll:
                _testClassToResult[_testClasses.Current].Result = new Result(
                    resultType, $"{resultType} in [{_stage}] method: {e.Message}"
                );
                PrintRich(
                    "[color=red]  {0}[/color]", _testClassToResult[_testClasses.Current].Result
                );
                _stage = TestStage.NotStarted;
                return true;
            case TestStage.BeforeEach:
            case TestStage.Test:
            case TestStage.AfterEach:
                _testFunctions.Current.TimeMilliseconds =
                    _stopwatch.ElapsedMilliseconds - _elapsedMilliseconds;
                _testFunctions.Current.Result = new Result(resultType, e.Message);
                PrintRich(
                    "[color=red]    {0} {1}s[/color]", _testFunctions.Current.Result,
                    _testFunctions.Current.TimeMilliseconds / 1000f
                );
                _stage = TestStage.BeforeEach;
                return true;
            default:
                return false;
        }
    }

    private void LoadTests()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
        foreach (Node node in _testList.GetChildren())
        {
            node.QueueFree();
        }

        TestLoader.LoadTestsInNodeAndChildren(_testClassToFunctions, GetTree().Root);
        _testClassToResult.Clear();
        foreach (TestClass testClass in _testClassToFunctions.Keys)
        {
            var testResultScene = GD.Load<PackedScene>(TestResult.ScenePath);
            var testResult = testResultScene.Instantiate<TestResult>();
            testResult.SetTypeName(testClass.Name);
            _testList.AddChild(testResult);
            _testClassToResult[testClass] = testResult;
            foreach (TestFunction function in _testClassToFunctions[testClass])
            {
                testResult.AddMethodResult(function);
            }
        }
        _stopwatch.Stop();
        Print(
            "{0} tests loaded in {1}ms", GetTree().CurrentScene.SceneFilePath,
            _stopwatch.ElapsedMilliseconds
        );
    }

    private void RunTests()
    {
        if (!_testClassToFunctions.Any())
        {
            Print("No tests loaded");
            return;
        }

        if (!_showHideButton.ElementsVisible)
        {
            _showHideButton.OnButtonPressed();
        }
        Visible = false; // Test GUI should not interfere with tests

        _testsRunning = true;
        _stage = TestStage.NotStarted;
        _testClasses = _testClassToFunctions.Keys.GetEnumerator();
        _elapsedMilliseconds = 0L;
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    private enum TestStage
    {
        NotStarted,
        BeforeAll,
        BeforeEach,
        Test,
        AfterEach,
        AfterAll,
        Finished
    }
}
