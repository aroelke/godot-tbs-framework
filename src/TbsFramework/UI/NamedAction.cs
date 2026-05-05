using System;
using Godot;

namespace TbsFramework.UI;

public readonly record struct NamedAction(StringName Name, Action Action);