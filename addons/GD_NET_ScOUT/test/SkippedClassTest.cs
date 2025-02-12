using Godot;

namespace GD_NET_ScOUT;

[Test]
[Skip("This class will intentionally fail if not skipped")]
public partial class SkippedClassTest : Node2D
{
    [BeforeAll]
    private void BeforeAll()
    {
        Assert.Fail("[Skip] attribute did not skip this class!");
    }

    [Test] private void Skipped1() {}

    [Test] private void Skipped2() {}

}
