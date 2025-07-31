using System.Reflection;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GD_NET_ScOUT;

public class TestFunction
{
    public string Name;
    public MethodInfo Method;
    public Result Result = Result.Success;
    public long TimeMilliseconds;
}
