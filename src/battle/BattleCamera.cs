using Godot;
using ui;

namespace battle;

/// <summary>Camera used for viewing the battle and through which it is interacted.</summary>
public partial class BattleCamera : Camera2D
{
	/// <summary>Only enable smooth scrolling when the mouse is used for control.</summary>
    /// <param name="mode">Cursor input mode being switched to.</param>
    public void OnInputModeChanged(InputMode mode)
    {
        PositionSmoothingEnabled = mode == InputMode.Mouse;
    }
}
