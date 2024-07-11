using System;
using System.Collections.Immutable;
using Godot;
using Scenes.Level.Object;

namespace Data;

[GlobalClass, Tool]
public partial class Faction : Resource
{
    private ImmutableDictionary<string, Faction> _allies = ImmutableDictionary<string, Faction>.Empty;

    [Export] public StringName Name = "";

    [Export] public Color Color = Colors.White;

    [Export(PropertyHint.TypeString, "4/13:*.tres" /* Variant.Type.String=4/PropertyHint.File=13 */)] public string[] AllyPaths = Array.Empty<string>();

    [Export] public bool IsPlayer = false;

    public ImmutableHashSet<Faction> Allies => (_allies = AllyPaths.ToImmutableDictionary((p) => p, (p) => _allies.ContainsKey(p) ? _allies[p] : ResourceLoader.Load<Faction>(p))).Values.ToImmutableHashSet();

    public bool AlliedTo(Faction other) => other == this || Allies.Contains(other);

    public bool AlliedTo(Unit unit) => AlliedTo(unit.Faction);
}