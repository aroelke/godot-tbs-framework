using System.Collections.Generic;
using Godot;
using ui;

namespace battle;

/// <summary>Main user interface of the battle screen for displaying control information and menus.</summary>
public partial class UserInterface : CanvasLayer
{
    private Controller _controller = null;
    private InputController _controlType = InputController.Mouse;

    private Dictionary<InputController, CanvasItem> _hints = null;

    private Controller Controller => _controller ??= GetNode<Controller>("/root/Controller");
    private Dictionary<InputController, CanvasItem> Hints => _hints ??= new()
    {
        { InputController.Mouse,       GetNode<CanvasItem>("HUD/Mouse") },
        { InputController.Keyboard,    GetNode<CanvasItem>("HUD/Keyboard") },
        { InputController.Playstation, GetNode<CanvasItem>("HUD/Playstation") }
    };

    /// <summary>When the input controller changes, update the controls hints to show the right buttons.</summary>
    /// <param name="controller">New input controller.</param>
    public void OnControllerChanged(InputController controller)
    {
        foreach ((InputController option, CanvasItem hint) in Hints)
            hint.Visible = option == controller;
    }

    public override void _Ready()
    {
        base._Ready();
        Controller.ControllerChanged += OnControllerChanged;
        OnControllerChanged(Controller.InputController);
    }
}
