using System.Linq;
using System.Reflection;

using Godot;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace GD_NET_ScOUT;

[Test]
public partial class TestRunnerTest : Node
{
    [Signal] public delegate void TestSignalEventHandler();

    private int? _beforeEachCount;
    private int? _afterEachCount;
    private int _frameCount;

    public override void _Process(double delta)
    {
        _frameCount++;
    }

    [BeforeAll]
    private void BeforeAll()
    {
        _beforeEachCount = 0;
        _afterEachCount = 0;
    }

    [AfterAll]
    private void AfterAll()
    {
        int numTests = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Count(
                method => method.GetCustomAttribute<TestAttribute>(false) != null
                    && method.GetCustomAttribute<SkipAttribute>(false) == null
            );
        Assert.AreEqual(numTests, _beforeEachCount);
        Assert.AreEqual(numTests, _afterEachCount);
    }

    [BeforeEach]
    private void BeforeEach()
    {
        _frameCount = 0;
        _beforeEachCount++;
    }

    [AfterEach]
    private void AfterEach()
    {
        _afterEachCount++;
    }

    [Test]
    private void VerifyBeforeAfterEachAll()
    {
        Assert.IsNotNull(_beforeEachCount);
        Assert.IsNotNull(_afterEachCount);
        Assert.AreEqual(_beforeEachCount, _afterEachCount + 1);
    }

    [Test]
    [Skip("This method will intentionally fail if not skipped")]
    private void MethodSkipAttribute()
    {
        Assert.Fail("[Skip] attribute did not skip this test!");
    }

    [Test]
    private void WaitFrames()
    {
        bool value = false;
        this.GetTestRunner().WaitFrames(1, () => Assert.IsFalse(value));
        this.GetTestRunner().WaitFrames(2, () => value = true);
        this.GetTestRunner()
            .WaitFrames(
                1,
                () => this.GetTestRunner()
                    .WaitFrames(
                        1, () => this.GetTestRunner().WaitFrames(1, () => Assert.IsTrue(value))
                    )
            );
    }

    [Test]
    private void WaitFrames_Count()
    {
        int frameCountStart = _frameCount;
        for (int i = 1; i < 10; i++)
        {
            int frames = i;
            this.GetTestRunner()
                .WaitFrames(i, () => Assert.AreEqual(frames, _frameCount - frameCountStart));
        }
    }

    [Test]
    private void WaitSeconds()
    {
        bool value = false;
        this.GetTestRunner()
            .WaitSeconds(
                0.33, () =>
                {
                    value = true;
                    this.GetTestRunner().WaitSeconds(0.33, () => value = false);
                }
            );
        this.GetTestRunner().WaitSeconds(0.5, () => Assert.IsTrue(value));
        this.GetTestRunner().WaitSeconds(0.75, () => Assert.IsFalse(value));
        Assert.IsFalse(value);
    }

    [Test]
    private void WaitForSignal()
    {
        bool value = false;
        this.GetTestRunner()
            .WaitForSignal(
                this, SignalName.TestSignal, () =>
                {
                    this.GetTestRunner()
                        .WaitForSignal(this, SignalName.TestSignal, () => value = true, 0.25);
                    // nested signal emission using TestRunner.InvokeNextFrame
                    this.GetTestRunner().InvokeNextFrame(() => EmitSignal(SignalName.TestSignal));
                }, 0.25
            );
        this.GetTestRunner().WaitSeconds(0.5, () => Assert.IsTrue(value));
        this.GetTestRunner().WaitSeconds(0.75); // ensure WaitForSignal timeouts do not trigger
        EmitSignal(SignalName.TestSignal);
        Assert.IsFalse(value);
    }
}
