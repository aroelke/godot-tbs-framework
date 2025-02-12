using System;

namespace GD_NET_ScOUT;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TestAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Method)]
public class BeforeEachAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Method)]
public class AfterEachAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Method)]
public class BeforeAllAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Method)]
public class AfterAllAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SkipAttribute : Attribute
{
    public readonly string Reason;
    public readonly bool Skip;

    public SkipAttribute(string reason = "", bool skip = true)
    {
        Reason = reason;
        Skip = skip;
    }
}
