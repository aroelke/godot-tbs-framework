using System.Collections.Generic;
using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

public abstract partial class ActionRange : Resource
{
    public abstract IEnumerable<Vector2I> GetAllCellsInRange(UnitData unit, Vector2I cell);

    public abstract IEnumerable<Vector2I> GetValidCellsInRange(UnitData unit, Vector2I cell);

    public virtual void Initialize(LevelManager manager) {}
}