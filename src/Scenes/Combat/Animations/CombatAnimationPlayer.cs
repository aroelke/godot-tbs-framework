using Godot;
using Scenes.Level.Object.Group;
using Nodes;

namespace Scenes.Combat.Animations;

/// <summary>Class containing combat animations for a character or class.  Has additional methods and signals for passing information to the <see cref="CombatScene"/>.</summary>
[GlobalClass, Tool]
public partial class CombatAnimationPlayer : AnimationPlayer
{
    private readonly NodeCache _cache;

    public CombatAnimationPlayer() : base()
    {
        _cache = new(this);
    }

    /// <summary>Signals that the camera should shake.</summary>
    [Signal] public delegate void ShakeCameraEventHandler();

    private Sprite2D Sprite => _cache.GetNodeOrNull<Sprite2D>("Sprite");

    /// <summary>Modulate color for the sprite to indicate which <see cref="Army"/> it belongs to.</summary>
    [Export] public Color Modulate
    {
        get => Sprite?.Modulate ?? Colors.White;
        set
        {
            if (Sprite is not null)
                Sprite.Modulate = value;
        }
    }
}