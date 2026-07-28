using System;
using Godot;

namespace TbsFramework.Scenes.Data;

public abstract partial class AbstractStats : Resource
{
    public event Action<AbstractStats> ValuesChanged;

    public abstract double MaxHealth { get; }

    public abstract int MoveDistance { get; }

    public abstract int GetTerrainCostModifier(Terrain terrain);

    public void SignalValuesChanged() { if (ValuesChanged is not null) ValuesChanged(this); }
}