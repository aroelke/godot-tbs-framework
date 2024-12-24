using Godot;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.AI;

/// <summary>
/// A <see cref="Unit"/> resource that provides information about how the AI uses it in a
/// specific situation.
/// </summary>
[GlobalClass, Tool]
public abstract partial class Behavior : Resource {}