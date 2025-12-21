using System.Collections.Immutable;
using Godot;
using TbsFramework.Scenes.Level.Object;

namespace TbsFramework.Data;

/// <summary>Defines global data about a faction in the game.</summary>
[GlobalClass, Tool]
public partial class Faction : Resource
{
    private ImmutableDictionary<string, Faction> _allies = ImmutableDictionary<string, Faction>.Empty;

    /// <summary>Name of the faction.</summary>
    [Export] public StringName Name = "";

    /// <summary>Primary color of the faction for easy distinguishing on the battlefield.</summary>
    [Export] public Color Color = Colors.White;

    /// <summary>Paths to other factions. Can't be direct references to them, as Godot doesn't support that.</summary>
    [Export(PropertyHint.TypeString, "4/13:*.tres" /* Variant.Type.String=4/PropertyHint.File=13 */)] public string[] AllyPaths = [];

    /// <summary>References to other factions that are allied to this one as loaded from <see cref="AllyPaths"/>.</summary>
    public ImmutableHashSet<Faction> Allies => (_allies = AllyPaths.ToImmutableDictionary(static (p) => p, (p) => _allies.TryGetValue(p, out Faction f) ? f : ResourceLoader.Load<Faction>(p))).Values.ToImmutableHashSet();

    /// <summary>Whether or not this faction is allied to another one.</summary>
    public bool AlliedTo(Faction other) => other == this || Allies.Contains(other);

    /// <summary>Whether or not this faction is allied to a <see cref="Unit"/>'s faction.</summary>
    public bool AlliedTo(Unit unit) => unit is not null && AlliedTo(unit.Army.Faction);
}