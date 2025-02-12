using Godot;

namespace GD_NET_ScOUT;

[Test]
public partial class AutoFailTest : Node2D
{
    public const string ScenePath = "res://addons/GD_NET_ScOUT/test/auto_fail_test.tscn";

    [Test]
    private void AutoFail()
    {
        Assert.Fail("This test always automatically fails.");
    }

}
