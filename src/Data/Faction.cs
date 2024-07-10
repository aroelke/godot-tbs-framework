using System;
using System.Linq;
using Godot;
using Scenes.Level.Object;

namespace Data;

[GlobalClass, Tool]
public partial class Faction : Resource
{
    private Faction[] _allies = null;

    [Export] public StringName Name = "";

    [Export] public Color Color = Colors.White;

    [Export(PropertyHint.TypeString, "4/13:*.tres" /* string is 4 and file is 13 */)] public string[] AllyPaths = Array.Empty<string>();

    [Export] public bool IsPlayer = false;

    public Faction[] Allies => _allies ??= AllyPaths.Select((p) => ResourceLoader.Load<Faction>(p)).ToArray();

    public bool AlliedTo(Faction other) => other == this || Allies.Contains(other);

    public bool AlliedTo(Unit unit) => AlliedTo(unit.Faction);
}