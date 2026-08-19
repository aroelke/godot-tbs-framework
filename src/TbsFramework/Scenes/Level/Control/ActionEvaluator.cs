using Godot;
using TbsFramework.Scenes.Data;

namespace TbsFramework.Scenes.Level.Control;

public record class SimulatedAction(GridData Grid, Faction Faction, Path Traversed, UnitAction Action)
{
    public UnitData GetActor() => Grid.Occupants[Traversed[^1]];
}

[GlobalClass, Tool]
public abstract partial class ActionEvaluator : Resource
{
    public abstract double Evaluate(SimulatedAction action);
}