using Godot;

namespace TbsTemplate.Scenes.Level.Control;

/// <summary>Generic action names to prevent proliferation of literals.</summary>
public static class UnitActions
{
    public static readonly StringName AttackAction = "Attack";
    public static readonly StringName SupportAction = "Support";
    public static readonly StringName EndAction = "End";
}