using Godot;

namespace Data;

/// <summary>Contains data pertaining to character classes, including stats and resources.</summary>
[GlobalClass, Tool]
public partial class Class : Resource
{
    private PackedScene _combatAnimation = null;

    /// <summary>Loaded (but not instantiated) combat animation scene for the class.</summary>
    public PackedScene CombatAnimations => LoadCombatAnimations();

    /// <summary>Path to the scene containing combat animations for the class.</summary>
    [Export(PropertyHint.File, "*.tscn")] public string CombatAnimationPath = "";

    /// <summary>Manually load the combat animation scene for control over when it gets loaded into memory.</summary>
    /// <returns>The loaded animation scene.</returns>
    public PackedScene LoadCombatAnimations() => _combatAnimation ??= ResourceLoader.Load<PackedScene>(CombatAnimationPath);
}