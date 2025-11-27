using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.Components;

namespace TbsTemplate.Data;

/// <summary>Contains data pertaining to character classes, including stats and resources.</summary>
[GlobalClass, Tool]
public partial class Class : Resource
{
    private class PackedSceneMap(IDictionary<Faction, string> paths, IDictionary<Faction, PackedScene> scenes, Func<Faction, PackedScene> loader) : IReadOnlyDictionary<Faction, PackedScene>
    {
        private void LoadAllScenes()
        {
            foreach (Faction faction in paths.Keys)
                loader(faction);
        }

        public PackedScene this[Faction key] => loader(key);

        public IEnumerable<Faction> Keys => paths.Keys;

        public IEnumerable<PackedScene> Values
        {
            get
            {
                LoadAllScenes();
                return scenes.Values;
            }
        }

        public int Count => paths.Count;

        public bool ContainsKey(Faction key) => paths.ContainsKey(key);

        public IEnumerator<KeyValuePair<Faction, PackedScene>> GetEnumerator()
        {
            LoadAllScenes();
            return scenes.GetEnumerator();
        }

        public bool TryGetValue(Faction key, [MaybeNullWhen(false)] out PackedScene value)
        {
            if (!paths.ContainsKey(key))
            {
                value = null;
                return false;
            }
            else
            {
                value = loader(key);
                return true;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static PackedScene LoadAnimations(Faction faction, IDictionary<Faction, string> paths, IDictionary<Faction, PackedScene> scenes)
    {
        if (paths.TryGetValue(faction, out string path))
        {
            if (scenes.TryGetValue(faction, out PackedScene scene))
                return scene;
            else
                return scenes[faction] = ResourceLoader.Load<PackedScene>(path);
        }
        else
            throw new ArgumentException($"Faction {faction.Name} does not have any map animations defined.");
    }

    private readonly Dictionary<Faction, PackedScene> _mapAnimations = [];
    private PackedScene _defaultMapAnimations = null;
    private readonly Dictionary<Faction, PackedScene> _combatAnimations = [];
    private PackedScene _defaultCombatAnimations = null;

    /// <summary>Mapping of factions onto respective paths to scenes defining map animations for units of this class.</summary>
    public Godot.Collections.Dictionary<Faction, string> MapAnimationsPaths = [];

    /// <summary>
    /// Mapping of factions onto respective scenes defining map animations for units of this class. Scenes are loaded from <see cref="MapAnimationsPaths"/>
    /// the first time each faction's is accessed.
    /// </summary>
    public IReadOnlyDictionary<Faction, PackedScene> MapAnimationsScenes { get; private set; } = null;

    /// <summary>
    /// Path to default map animation scene for units of this class if their <see cref="Faction"/> isn't present in <see cref="MapAnimationsPaths"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    public string DefaultMapAnimationsPath = "";

    /// <summary>
    /// Default map animation scene for units of this class if their <see cref="Faction"/> isn't present in <see cref="MapAnimationsPaths"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    public PackedScene DefaultMapAnimationsScene => LoadDefaultMapAnimations();

    /// <summary>Mapping of factions on to respective paths to scenes defining combat animations for units of this class.</summary>
    public Godot.Collections.Dictionary<Faction, string> CombatAnimationsPaths = [];

    /// <summary>
    /// Mapping of factions onto respective scenes defining combat animations for units of this class. Scenes are loaded from <see cref="CombatAnimationsPaths"/>
    /// the first time each faction's is accessed.
    /// </summary>
    public IReadOnlyDictionary<Faction, PackedScene> CombatAnimationsScenes { get; private set; } = null;

    /// <summary>
    /// Path to default combat animation for units of this class if their <see cref="Faction"/> isn't present in <see cref="CombatAnimationsPaths"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    public string DefaultCombatAnimationsPath = "";

    /// <summary>
    /// Default combat animation for units of this class if their <see cref="Faction"/> isn't present in <see cref="CombatAnimationsPaths"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    public PackedScene DefaultCombatAnimationsScene => LoadDefaultCombatAnimations();

    /// <summary>Mapping of factions onto sprites to display for units of this class in the editor.</summary>
    [Export] public Godot.Collections.Dictionary<Faction, Texture2D> EditorSprites = [];

    /// <summary>
    /// Default sprite to use for units of this class in the editor if their <see cref="Faction"/> isn't present in <see cref="EditorSprites"/>.
    /// Colorize using <see cref="Faction.Color"/> to differentiate.
    /// </summary>
    [Export] public Texture2D DefaultEditorSprite = null;

    public Class() : base()
    {
        MapAnimationsScenes = new PackedSceneMap(MapAnimationsPaths, _mapAnimations, LoadMapAnimations);
        CombatAnimationsScenes = new PackedSceneMap(CombatAnimationsPaths, _combatAnimations, LoadCombatAnimations);
    }

    /// <summary>Manually load map animations for units of this class belonging to a faction.</summary>
    /// <param name="faction">Faction whose animations should be loaded.</param>
    /// <returns>Scene defining map animations.</returns>
    public PackedScene LoadMapAnimations(Faction faction) => LoadAnimations(faction, MapAnimationsPaths, _mapAnimations);

    /// <summary>Manually load the default map animation scene for units of this class.</summary>
    /// <returns>The loaded animation scene.</returns>
    public PackedScene LoadDefaultMapAnimations() => _defaultMapAnimations ??= ResourceLoader.Load<PackedScene>(DefaultMapAnimationsPath);

    /// <summary>Manually load combat animations for units of this class belonging to a faction.</summary>
    /// <param name="faction">Faction whose animations should be loaded.</param>
    /// <returns>Scene defining combat animations.</returns>
    public PackedScene LoadCombatAnimations(Faction faction) => LoadAnimations(faction, CombatAnimationsPaths, _combatAnimations);

    /// <summary>Manually load the default combat animation scene for units of this class.</summary>
    /// <returns>The loaded animation scene.</returns>
    public PackedScene LoadDefaultCombatAnimations() => _defaultCombatAnimations ??= ResourceLoader.Load<PackedScene>(DefaultCombatAnimationsPath);

    /// <summary>Create an instance of the map animations for a member of this class and of a particular faction.</summary>
    /// <param name="faction">Faction to instantiate the map animations for.</param>
    /// <returns>
    /// The map animations for units of this class and <paramref name="faction"/>. If <paramref name="faction"/> does not have defined map animations,
    /// return an instance of <see cref="DefaultMapAnimationsPath"/> instead.
    /// </returns>
    public UnitMapAnimations InstantiateMapAnimations(Faction faction)
    {
        if (faction is not null && MapAnimationsPaths.ContainsKey(faction))
            return MapAnimationsScenes[faction].Instantiate<UnitMapAnimations>();
        else
        {
            UnitMapAnimations animations = DefaultMapAnimationsScene.Instantiate<UnitMapAnimations>();
            if (faction is not null)
                animations.Modulate = faction.Color;
            return animations;
        }
    }

    /// <summary>Create an instance of the combat animations for a member of this class and of a particular faction.</summary>
    /// <param name="faction">Faction to instantiate the combat animations for.</param>
    /// <returns>
    /// The combat animations for units of this class and <paramref name="faction"/>. If <paramref name="faction"/> does not have defined map animations,
    /// return an instance of <see cref="DefaultCombatAnimationsPath"/> instead.</returns>
    public CombatAnimations InstantiateCombatAnimations(Faction faction)
    {
        if (faction is not null && CombatAnimationsPaths.ContainsKey(faction))
            return CombatAnimationsScenes[faction].Instantiate<CombatAnimations>();
        else
        {
            CombatAnimations animations = DefaultCombatAnimationsScene.Instantiate<CombatAnimations>();
            if (faction is not null)
                animations.Modulate = faction.Color;
            return animations;
        }
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = [.. base._GetPropertyList() ?? []];

        properties.AddRange([
            new ObjectProperty(
                PropertyName.MapAnimationsPaths,
                Variant.Type.Dictionary,
                PropertyHint.TypeString,
                $"{Variant.Type.Object:D}/{PropertyHint.ResourceType:D}:Faction;{Variant.Type.String:D}/{PropertyHint.File:D}:*.tscn"
            ),
            new ObjectProperty(PropertyName.DefaultMapAnimationsPath, Variant.Type.String, PropertyHint.File, "*.tscn"),
            new ObjectProperty(
                PropertyName.CombatAnimationsPaths,
                Variant.Type.Dictionary,
                PropertyHint.TypeString,
                $"{Variant.Type.Object:D}/{PropertyHint.ResourceType:D}:Faction;{Variant.Type.String:D}/{PropertyHint.File:D}:*.tscn"
            ),
            new ObjectProperty(PropertyName.DefaultCombatAnimationsPath, Variant.Type.String, PropertyHint.File, "*.tscn")
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