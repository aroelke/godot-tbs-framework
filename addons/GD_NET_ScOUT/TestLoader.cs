using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Godot;

namespace GD_NET_ScOUT;

internal static class TestLoader
{
    public static void LoadTestsInNodeAndChildren(
        IDictionary<TestClass, TestFunction[]> tests, Node root)
    {
        tests.Clear();
        Dictionary<Type, Node> nodes = new();
        AccumulateNodes(nodes, root);
        LoadFunctionsFromNodes(tests, nodes);
    }

    private static void AccumulateNodes(IDictionary<Type, Node> nodes, Node root)
    {
        if (root.GetType().GetCustomAttribute<TestAttribute>(false) is not null)
        {
            nodes.Add(root.GetType(), root);
        }
        foreach (Node child in root.GetChildren())
        {
            AccumulateNodes(nodes, child);
        }
    }

    private static void LoadFunctionsFromNodes(
        IDictionary<TestClass, TestFunction[]> tests, IDictionary<Type, Node> nodes)
    {
        foreach (KeyValuePair<Type, Node> entry in nodes)
        {
            TestAttribute? testAttribute = entry.Key.GetCustomAttribute<TestAttribute>(false);
            SkipAttribute? skipAttribute = entry.Key.GetCustomAttribute<SkipAttribute>(false);
            if (testAttribute is null)
            {
                continue;
            }

            TestClass testClass = new TestClass
            {
                Name = entry.Key.Name, Type = entry.Key, Instance = entry.Value
            };
            if (skipAttribute is {Skip: true})
            {
                testClass.Result = new Result(Result.Type.Skipped, skipAttribute.Reason);
            }
            tests.Add(testClass, LoadFunctionsFromType(testClass).ToArray());
        }
    }

    private static IEnumerable<TestFunction> LoadFunctionsFromType(TestClass testClass)
    {
        foreach (MethodInfo method in testClass.Type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            ))
        {
            BeforeEachAttribute? beforeEach = method.GetCustomAttribute<BeforeEachAttribute>(false);
            AfterEachAttribute? afterEach = method.GetCustomAttribute<AfterEachAttribute>(false);
            BeforeAllAttribute? beforeAll = method.GetCustomAttribute<BeforeAllAttribute>(false);
            AfterAllAttribute? afterAll = method.GetCustomAttribute<AfterAllAttribute>(false);
            TestAttribute? testAttribute = method.GetCustomAttribute<TestAttribute>(false);
            SkipAttribute? skipAttribute = method.GetCustomAttribute<SkipAttribute>(false);

            if (new object?[] {beforeEach, afterEach, beforeAll, afterAll, testAttribute}.Count(
                    o => o is not null
                )
                > 1)
            {
                GD.PushError(
                    $"Method {method.Name} in {testClass.Type} has too many attributes. Skipping..."
                );
                continue;
            }

            if (TestClassMethodHelper(method, beforeEach, ref testClass.BeforeEach)
                || TestClassMethodHelper(method, afterEach, ref testClass.AfterEach)
                || TestClassMethodHelper(method, beforeAll, ref testClass.BeforeAll)
                || TestClassMethodHelper(method, afterAll, ref testClass.AfterAll))
            {
                continue;
            }

            if (TestClassMethodHelper(method, testAttribute))
            {
                continue;
            }

            TestFunction testFunction = new TestFunction {Name = method.Name, Method = method};
            if (skipAttribute is {Skip: true})
            {
                testFunction.Result = new Result(Result.Type.Skipped, skipAttribute.Reason);
            }
            yield return testFunction;
        }
    }

    private static bool TestClassMethodHelper<T>(
        MethodInfo method, T? attribute, ref MethodInfo? outMethod) where T : Attribute
    {
        if (attribute is null)
        {
            return false;
        }

        if (TestClassMethodHelper(method, attribute))
        {
            return true;
        }

        if (outMethod is not null)
        {
            GD.PushError(
                $"{nameof(T)} method {nameof(outMethod)} already defined in {method.DeclaringType}. Skipping..."
            );
            return true;
        }

        outMethod = method;
        return false;
    }

    private static bool TestClassMethodHelper<T>(MethodInfo method, T? attribute)
    {
        if (attribute is null)
        {
            return true;
        }

        if (method.ReturnType != typeof(void))
        {
            GD.PushError(
                $"{nameof(T)} method {method.Name} in {method.DeclaringType} does not return void. Skipping..."
            );
            return true;
        }

        return false;
    }
}
