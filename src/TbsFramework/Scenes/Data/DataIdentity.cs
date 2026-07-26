using Godot;
using TbsFramework.Scenes.Level.Control;

namespace TbsFramework.Scenes.Data;

/// <summary>
/// Represents the "idenitity" of an object used by this framework. Mainly used to provide a means of referring to objects across copies
/// of the grid created by <see cref="AIController"/>. Note that this is different from a normal object reference in that it can refer to
/// multiple things depending on context.
/// </summary>
/// <typeparam name="T">Type of object this identity corresponds to.</typeparam>
[Tool]
public abstract partial class DataIdentity<T> : Resource {}