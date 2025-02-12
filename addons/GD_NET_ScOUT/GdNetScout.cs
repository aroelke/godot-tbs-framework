#if TOOLS
using Godot;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GD_NET_ScOUT_TOOL;

[Tool]
public partial class GdNetScout : EditorPlugin
{
    private const string AutoloadName = "SceneScout";
    private const string AutoloadPath = "res://addons/GD_NET_ScOUT/SceneScout.cs";

    [Export] private Control _dock;

    public override void _EnterTree()
    {
        AddAutoloadSingleton(AutoloadName, AutoloadPath);
        _dock = GD.Load<PackedScene>(Dock.ScenePath).Instantiate<Control>();
        AddControlToDock(DockSlot.RightUl, _dock);
    }

    public override void _ExitTree()
    {
        RemoveControlFromDocks(_dock);
        _dock.Free();
        RemoveAutoloadSingleton(AutoloadName);
    }
}
#endif
