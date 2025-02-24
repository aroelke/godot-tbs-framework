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

    [Test] public void TestOneElementPermutation() => TestPermutations([1], [[1]]);
    [Test] public void TestTwoElementPermutation() => TestPermutations([1, 2], [[1, 2], [2, 1]]);
    [Test] public void TestThreeElementPermutation() => TestPermutations([1, 2, 3], [[1, 2, 3], [1, 3, 2], [2, 1, 3], [2, 3, 1], [3, 1, 2], [3, 2, 1]]);
    [Test] public void TestFourElementPermutation() => TestPermutations([1, 2, 3, 4], [
        [1, 2, 3, 4], [1, 2, 4, 3], [1, 3, 2, 4], [1, 3, 4, 2], [1, 4, 2, 3], [1, 4, 3, 2],
        [2, 1, 3, 4], [2, 1, 4, 3], [2, 3, 1, 4], [2, 3, 4, 1], [2, 4, 1, 3], [2, 4, 3, 1],
        [3, 1, 2, 4], [3, 1, 4, 2], [3, 2, 1, 4], [3, 2, 4, 1], [3, 4, 1, 2], [3, 4, 2, 1],
        [4, 1, 2, 3], [4, 1, 3, 2], [4, 2, 1, 3], [4, 2, 3, 1], [4, 3, 1, 2], [4, 3, 2, 1]
    ]);
}