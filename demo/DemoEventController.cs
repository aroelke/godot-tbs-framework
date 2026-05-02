using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsFramework.Extensions;
using TbsFramework.Scenes;
using TbsFramework.Scenes.Level.Events;
using TbsFramework.Scenes.Rendering;
using TbsFramework.UI;
using TbsFramework.UI.Controls.Device;

namespace TbsFramework.Demo;

/// <summary>
/// Demo implementation of an event controller.  Reacts to objective completion and sends the player to the game over screen
/// when one is completed.
/// </summary>
public partial class DemoEventController : EventController
{
    private ContextMenu _menu = null;
    private Vector2I _menuCell = -Vector2I.One;

    private Vector2 MenuPosition(Rect2 rect, Vector2 size)
    {
        Rect2 viewportRect = Grid.GetGlobalTransformWithCanvas()*rect;
        float viewportCenter = GetViewport().GetVisibleRect().Position.X + GetViewport().GetVisibleRect().Size.X/2;
        return new(
            viewportCenter - viewportRect.Position.X < viewportRect.Size.X/2 ? viewportRect.Position.X - size.X : viewportRect.End.X,
            Mathf.Clamp(viewportRect.Position.Y - (size.Y - viewportRect.Size.Y)/2, 0, GetViewport().GetVisibleRect().Size.Y - size.Y)
        );
    }

    [Export] public Grid Grid = null;
    [Export] public CanvasLayer UserInterface = null;
    [Export(PropertyHint.File, "*.tscn")] public string GameOverScreen = null;
    [Export] public AudioStream MenuHighlightSound = null;

    public async void OnObjectiveCompleted(bool success)
    {
        await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);

        SceneManager.Singleton.Connect<DemoGameOverScene>(SceneManager.SignalName.SceneLoaded, (s) => {
            s.win = success;
            QueueFree();
        }, (uint)ConnectFlags.OneShot);
        SceneManager.JumpToScene(GameOverScreen);
    }

    public void OnSuccessObjectiveCompleted() => OnObjectiveCompleted(true);
    public void OnFailureObjectiveCompleted() => OnObjectiveCompleted(false);

    public void OnMenuShown(Vector2I cell, IEnumerable<ContextMenuOption> options, Action canceled, Action @finally)
    {
        _menuCell = cell;
        _menu = ContextMenu.Instantiate(options, MenuHighlightSound);
        _menu.Wrap = true;
        UserInterface.AddChild(_menu);
        _menu.Visible = false;
        _menu.MenuCanceled += () => canceled();
        _menu.MenuClosed += () => {
            _menu = null;
            _menuCell = -Vector2I.One;
            @finally();
        };

        Callable.From<ContextMenu, Rect2>((m, r) => {
            m.Visible = true;
            if (DeviceManager.Mode != InputMode.Mouse)
                m.GrabFocus();
            m.Position = MenuPosition(r, m.Size);
        }).CallDeferred(_menu, Grid.CellRect(cell));
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.SuccessObjectiveCompleted += OnSuccessObjectiveCompleted;
            LevelEvents.FailureObjectiveCompleted += OnFailureObjectiveCompleted;
            LevelEvents.ActionsPresented += OnMenuShown;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_menu is not null)
            _menu.Position = MenuPosition(Grid.CellRect(_menuCell), _menu.Size);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (!Engine.IsEditorHint())
        {
            LevelEvents.SuccessObjectiveCompleted -= OnSuccessObjectiveCompleted;
            LevelEvents.FailureObjectiveCompleted -= OnFailureObjectiveCompleted;
            LevelEvents.ActionsPresented -= OnMenuShown;
        }
    }
}