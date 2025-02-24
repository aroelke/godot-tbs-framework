using System;
using System.Collections.Generic;
using System.Linq;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Extensions;

namespace TbsTemplate.MathExt.Test;

[Test]
public partial class CollectionsTestScene : Node
{
    private static void TestPermutations<T>(IEnumerable<T> collection, IEnumerable<IList<T>> expected)
    {
        IList<IList<T>> permutations = [.. collection.Permutations()];
        Assert.AreEqual(permutations.Count, expected.Count());
        foreach (IList<T> test in expected)
        {
            IList<T> found = null;
            foreach (IList<T> permutation in permutations)
            {
                if (Enumerable.SequenceEqual(permutation, test))
                {
                    found = permutation;
                    break;
                }
            }
            Assert.IsNotNull(found, $"Permutation [{string.Join(',', test.Select((e) => e.ToString()))}] not found.");
            permutations.Remove(found);
        }
        Assert.IsTrue(permutations.Count == 0, "There were extra permutations in the list");
    }

    [Test] public void TestEmptyPermutations() => Assert.IsTrue(!Array.Empty<int>().Permutations().Any());
    [Test] public void TestOneElementPermutation() => TestPermutations([1], [[1]]);
    [Test] public void TestTwoElementPermutation() => TestPermutations([1, 2], [[1, 2], [2, 1]]);
    [Test] public void TestThreeElementPermutation() => TestPermutations([1, 2, 3], [[1, 2, 3], [1, 3, 2], [2, 1, 3], [2, 3, 1], [3, 1, 2], [3, 2, 1]]);
    [Test] public void TestFourElementPermutation() => TestPermutations([1, 2, 3, 4], [
        [1, 2, 3, 4], [1, 2, 4, 3], [1, 3, 2, 4], [1, 3, 4, 2], [1, 4, 2, 3], [1, 4, 3, 2],
        [2, 1, 3, 4], [2, 1, 4, 3], [2, 3, 1, 4], [2, 3, 4, 1], [2, 4, 1, 3], [2, 4, 3, 1],
        [3, 1, 2, 4], [3, 1, 4, 2], [3, 2, 1, 4], [3, 2, 4, 1], [3, 4, 1, 2], [3, 4, 2, 1],
        [4, 1, 2, 3], [4, 1, 3, 2], [4, 2, 1, 3], [4, 2, 3, 1], [4, 3, 1, 2], [4, 3, 2, 1]
    ]);

    private static void TestCross<T, U>(IEnumerable<T> a, IEnumerable<U> b, IEnumerable<(T, U)> expected)
    {
        IList<(T, U)> cross = [.. a.Cross(b)];
        Assert.AreEqual(cross.Count, expected.Count());
        foreach ((T, U) test in expected)
        {
            Assert.IsTrue(cross.Contains(test));
            cross.Remove(test);
        }
        Assert.IsTrue(cross.Count == 0, "There were extra elements in the cross product");
    }

    [Test] public void TestOneCrossOne() => TestCross([1], ['a'], [(1, 'a')]);
    [Test] public void TestOneCrossTwo() => TestCross([1], ['a', 'b'], [(1, 'a'), (1, 'b')]);
    [Test] public void TestTwoCrossOne() => TestCross([1, 2], ['a'], [(1, 'a'), (2, 'a')]);
    [Test] public void TestTwoCrossTwo() => TestCross([1, 2], ['a', 'b'], [(1, 'a'), (1, 'b'), (2, 'a'), (2, 'b')]);
    [Test] public void TestTwoCrossThree() => TestCross([1, 2], ['a', 'b', 'c'], [(1, 'a'), (1, 'b'), (1, 'c'), (2, 'a'), (2, 'b'), (2, 'c')]);
    [Test] public void TestThreeCrossTwo() => TestCross([1, 2, 3], ['a', 'b'], [(1, 'a'), (1, 'b'), (2, 'a'), (2, 'b'), (3, 'a'), (3, 'b')]);
}