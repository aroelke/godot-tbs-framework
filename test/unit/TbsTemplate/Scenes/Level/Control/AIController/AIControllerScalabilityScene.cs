using System.Collections.Generic;
using GD_NET_ScOUT;
using Godot;
using TbsTemplate.Scenes.Level.Map;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;

namespace TbsTemplate.Scenes.Level.Control.Test;

[Test]
public partial class AIControllerScalabilityScene : Node
{
    [Test]
    public void TestAIScalability()
    {
        Grid grid = GetNode<Grid>("Grid");

        IEnumerable<Unit> allies  = [.. (IEnumerable<Unit>)GetNode<Army>("Army1")];
        IEnumerable<Unit> enemies = [.. (IEnumerable<Unit>)GetNode<Army>("Army2")];
        foreach (Unit unit in allies)
            grid.Occupants[unit.Cell] = unit;
        foreach (Unit unit in enemies)
            grid.Occupants[unit.Cell] = unit;

        (Unit selected, Vector2I destination, StringName action, Unit target) = GetNode<AIController>("Army1/AIController").ComputeAction(allies, [], grid);
        GD.Print($"Move {selected.Faction.Name}@{selected.Cell} to {destination} and {action} {(target is not null ? $"{target.Faction.Name}@{target.Cell}" : "")}");
    }
}