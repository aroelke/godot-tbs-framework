using Godot;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

[GlobalClass]
public abstract partial class ActionDomain : Resource
{
    public abstract bool Contains(Vector2I cell);

    public virtual void Initialize(LevelManager manager) {}
}