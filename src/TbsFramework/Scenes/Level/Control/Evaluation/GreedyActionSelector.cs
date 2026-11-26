using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control.Evaluation;

/// <summary>
/// Depth-first action selection algorithm that explores the entire action tree. An action's value is accumulated with the best value from among subsequent actions in order to prioritize
/// earlier performance of high-value actions.
/// </summary>
[GlobalClass, Tool]
public partial class GreedyActionSelector : ActionSelector
{
    private class GridDataEqualityComparer(int seed = 31333, int mod = 227) : IEqualityComparer<GridData>
    {
        public bool Equals(GridData x, GridData y)
        {
            if (x.Occupants.Count != y.Occupants.Count)
                return false;
            static int comparison(Vector2I a, Vector2I b) => a < b ? -1 : a == b ? 0 : 1;
            IEnumerable<UnitData> xOccupants = x.Occupants.Values.OrderBy((u) => u.Cell, comparison);
            IEnumerable<UnitData> yOccupants = y.Occupants.Values.OrderBy((u) => u.Cell, comparison);
            foreach ((UnitData a, UnitData b) in xOccupants.Zip(yOccupants))
                if (a.Active != b.Active || a.Cell != b.Cell || a.Identity != b.Identity || a.Health != b.Health)
                    return false;
            return true;
        }

        public int GetHashCode([DisallowNull] GridData obj) => obj.Occupants.Values.Select((u) => HashCode.Combine(u.Active, u.Cell, u.Identity, u.Health)).Aggregate(seed, (a, b) => mod*a + b);
    }

    private double GetActionValue(SpecifiedAction action, IEnumerable<UnitData> units, IEnumerable<UnitAction> available, int remaining, Dictionary<GridData, double> cache)
    {
        GridData result = action.Action.Simulate(action.Actor, action.Destination, action.Target);
        if (result.Occupants.TryGetValue(action.Destination, out UnitData actor))
            actor.Active = false;
        if (!cache.TryGetValue(result, out double value))
        {
            value = GetActionValue(action, result);
            if ((remaining <= 0 || remaining > 1) && NextValueCoefficient != 0)
            {
                IEnumerable<UnitData> next = units.Select((u) => result.Occupants.Values.FirstOrDefault((o) => o.Identity == u.Identity, null)).Where((u) => u is not null && u.Active);
                value = CurrentValueCoefficient*value + NextValueCoefficient*GetAvailableActions(next, available).Select((a) => GetActionValue(a, next, available, remaining - 1, cache)).DefaultIfEmpty(0).Max();
            }
            cache[result] = value;
        }
        return value;
    }

    /// <summary>Maximum depth into the action tree to search.</summary>
    [Export(PropertyHint.Range, "0,5,1,or_greater")] public int MaxSearchDepth = 3;

    /// <summary>Coefficient to apply to the current action value being computed during value accumulation.</summary>
    [Export] public double CurrentValueCoefficient = 1;

    /// <summary>Coefficient to apply to the next level's value during value accumulation.</summary>
    [Export] public double NextValueCoefficient = 1;

    /// <summary>Whether or not to evaluate the value of each action in the current grid on a separate thread.</summary>
    [Export] public bool EvaluateWithThreads = false;

    public override SpecifiedAction? SelectAction(IEnumerable<UnitData> units, IEnumerable<UnitAction> available)
    {
        if (!units.Any())
            throw new ArgumentException("No units to select an action for");

        IEnumerable<SpecifiedAction> actions = GetAvailableActions(units, available);
        Func<double> task(SpecifiedAction action, Dictionary<GridData, double> cache) => () => GetActionValue(action, units, available, MaxSearchDepth, cache);
        if (actions.Any())
        {
            if (EvaluateWithThreads)
                return actions.Zip(Task.WhenAll([.. actions.Select((a) => Task.Run(task(a, new Dictionary<GridData, double>(new GridDataEqualityComparer()))))]).Result).MaxBy((a) => a.Second).First;
            else
            {
                Dictionary<GridData, double> cache = new(new GridDataEqualityComparer());
                return actions.MaxBy((a) => task(a, cache)());
            }
        }
        else
            return null;
    }
}