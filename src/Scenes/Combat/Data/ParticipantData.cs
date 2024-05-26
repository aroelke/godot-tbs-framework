using Godot;
using Scenes.Level.Object;

namespace Scenes.Combat.Data;

/// <summary>Constant information about a <see cref="Level.Object.Unit"/> participating in combat.</summary>
public readonly struct ParticipantData
{
    /// <summary>The actual unit performing actions, which supplies animations.</summary>
    public readonly Unit Unit = null;

    /// <summary>
    /// Displayed chance the unit will hit its target. Use a negative number to indicate that this doesn't apply to an action, such as healing.
    /// If it does apply, values should be 100 or less.
    /// </summary>
    /// <remarks>For display only, as the actual result of each attack is pre-computed.</remarks>
    public readonly int HitChance = 0;

    public ParticipantData(Unit unit = null, int hit = 0)
    {
        Unit = unit;
        HitChance = hit;

        if (hit > 100)
            GD.PushWarning($"Hit chance of {unit.Name} is greater than 100%");
    }
}