using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass]
public abstract partial class ActionPermission : Resource
{
    public abstract bool CanPerform(UnitData unit);

    public virtual void Initialize(LevelManager manager) {}
}