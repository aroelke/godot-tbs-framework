using System;
using System.Reflection;

using Godot;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GD_NET_ScOUT;

public class TestClass
{
    public string Name;
    public Type Type;
    public MethodInfo? BeforeEach;
    public MethodInfo? AfterEach;
    public MethodInfo? BeforeAll;
    public MethodInfo? AfterAll;
    public Node Instance;
    public Result Result = Result.Success;
}
