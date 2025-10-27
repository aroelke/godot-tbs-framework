using Godot;
using TbsTemplate.Extensions;

namespace TbsTemplate.Data;

/// <summary>Contains data pertaining to character classes, including stats and resources.</summary>
[GlobalClass, Tool]
public partial class Class : Resource
{
    private PackedScene _combatAnimation = null;

    /// <summary>Loaded (but not instantiated) combat animation scene for the class.</summary>
    public PackedScene CombatAnimations => LoadCombatAnimations();

    /// <summary>Mapping of factions onto respective scenes defining map animations for units of this class.</summary>
    public Godot.Collections.Dictionary<Faction, string> MapAnimationsPaths = [];

    /// <summary>
    /// Default map animation scene for units of this class if their <see cref="Faction"/> isn't present in <see cref="MapAnimationsPaths"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    public string DefaultMapAnimationsPath = "";

    /// <summary>Mapping of factions on to respective scenes defining combat animations for units of this class.</summary>
    public Godot.Collections.Dictionary<Faction, string> CombatAnimationsPaths = [];

    /// <summary>
    /// Default combat animation for units of this class if their <see cref="Faction"/> isn't present in <see cref="CombatAnimationsPaths"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    public string DefaultCombatAnimationsPath = "";

    /// <summary>Mapping of factions onto sprites to display for units of this class in the editor.</summary>
    [Export] public Godot.Collections.Dictionary<Faction, Texture2D> EditorSprites = [];

    /// <summary>
    /// Default sprite to use for units of this class in the editor if their <see cref="Faction"/> isn't present in <see cref="EditorSprites"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    [Export] public Texture2D DefaultEditorSprite = null;

    /// <summary>Path to the scene containing combat animations for the class.</summary>
    [Export(PropertyHint.File, "*.tscn")] public string CombatAnimationPath = "";

    /// <summary>Manually load the combat animation scene for control over when it gets loaded into memory.</summary>
    /// <returns>The loaded animation scene.</returns>
    public PackedScene LoadCombatAnimations() => _combatAnimation ??= ResourceLoader.Load<PackedScene>(CombatAnimationPath);

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = [.. base._GetPropertyList() ?? []];

        properties.AddRange([
            new ObjectProperty(
                PropertyName.CombatAnimationsPaths,
                Variant.Type.Dictionary,
                PropertyHint.TypeString,
                $"{Variant.Type.Object:D}/{PropertyHint.ResourceType:D}:Faction;{Variant.Type.String:D}/{PropertyHint.File:D}:*.tscn"
            ),
            new ObjectProperty(PropertyName.DefaultCombatAnimationsPath, Variant.Type.String, PropertyHint.File, "*.tscn"),
            new ObjectProperty(
                PropertyName.MapAnimationsPaths,
                Variant.Type.Dictionary,
                PropertyHint.TypeString,
                $"{Variant.Type.Object:D}/{PropertyHint.ResourceType:D}:Faction;{Variant.Type.String:D}/{PropertyHint.File:D}:*.tscn"
            ),
            new ObjectProperty(PropertyName.DefaultMapAnimationsPath, Variant.Type.String, PropertyHint.File, "*.tscn")
        ]);

        return properties;
    }

    public override bool _PropertyCanRevert(StringName property) => base._PropertyCanRevert(property) ||
        property == PropertyName.MapAnimationsPaths ||
        property == PropertyName.DefaultMapAnimationsPath ||
        property == PropertyName.CombatAnimationsPaths ||
        property == PropertyName.DefaultCombatAnimationsPath;

    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == PropertyName.MapAnimationsPaths       || property == PropertyName.CombatAnimationsPaths)
            return new Godot.Collections.Dictionary<Faction, string>();
        if (property == PropertyName.DefaultMapAnimationsPath || property == PropertyName.DefaultCombatAnimationsPath)
            return "";
        return base._PropertyGetRevert(property);
    }
}