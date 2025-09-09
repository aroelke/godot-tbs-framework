using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TbsTemplate.Extensions;
using TbsTemplate.Nodes.Components;
using TbsTemplate.UI.Controls.Device;

namespace TbsTemplate.UI;

/// <summary><see cref="ContextMenu"/> item defining a name and action to perform when clicked.</summary>
/// <param name="Name">String to display for the menu item.</param>
/// <param name="Action">What to do when the item is selected.</param>
public readonly record struct ContextMenuOption(StringName Name, Action Action);

/// <summary>
/// A context menu consisting of a vertical list of <see cref="Button"/>s.  Sends a signal when one of them is clicked, but also exposes them
/// individually if needed.  Either way, automatically frees itself once a button is clicked.  Menu can also be canceled without selecting an
/// item.  Not meant to be reused and options list is not meant to be modified (even though the individual options can be).
/// </summary>
[Tool]
public partial class ContextMenu : PanelContainer
{
    /// <summary>Signals that one of the items has been selected.</summary>
    /// <param name="item">Name of the item that was selected.</param>
    [Signal] public delegate void ItemSelectedEventHandler(StringName item);

    /// <summary>Signals that the menu has been canceled without selecting an item.</summary>
    [Signal] public delegate void MenuCanceledEventHandler();

    /// <summary>
    /// Signals that the menu is being closed, whether an option is selected or it was canceled. Emitted after the respective action signal is
    /// emitted.
    /// </summary>
    [Signal] public delegate void MenuClosedEventHandler();

    private const int NothingSelected = -1;
    private const string ScenePath = "res://src/TbsTemplate/UI/ContextMenu.tscn";
    private static PackedScene Scene = null;

    /// <summary>Set up a context menu with a set of options mapped to actions.</summary>
    /// <param name="options">List of options to show and their actions.</param>
    public static ContextMenu Instantiate(IEnumerable<ContextMenuOption> options)
    {
        ContextMenu menu = (Scene ??= GD.Load<PackedScene>(ScenePath)).Instantiate<ContextMenu>();

        menu.Options = [.. options.Select((o) => o.Name)];
        // Do this with the ready signal to make sure the menu has been initialized (ready is emitted after _Ready is called)
        menu.Connect(SignalName.Ready, () => {
            foreach ((StringName name, Action action) in options)
                menu._items[name].Pressed += action;
        }, (uint)ConnectFlags.OneShot);

        return menu;
    }

    private readonly NodeCache _cache = null;
    private StringName[] _options = [];
    private readonly Dictionary<StringName, Button> _items = [];
    private int _selected = NothingSelected;
    private int _hovered = NothingSelected;
    private int _focus = NothingSelected;
    private bool _suppress = false;

    private GridContainer Items => _cache.GetNode<GridContainer>("Items");
    private AudioStreamPlayer HighlightSound => _cache.GetNode<AudioStreamPlayer>("HighlightSound");

    private void UpdateItems()
    {
        if (Items is not null)
        {
            foreach (Button child in Items.GetChildren().OfType<Button>())
                child.QueueFree();
            foreach (StringName option in _options)
                Items.AddChild(_items[option] = new() { Text = option });
        }
    }

    /// <summary>Get the button representing the item with the given name.</summary>
    /// <param name="option">Name of the item to get.</param>
    public Button this[StringName option] => _items[option];

    [Export] public StringName[] Options
    {
        get => _options;
        private set
        {
            if (_options != value)
            {
                _options = value;
                _items.Clear();
                if (Engine.IsEditorHint())
                    UpdateItems();
            }
        }
    }

    /// <summary>
    /// Whether or not pressing up or down at the top or bottom of the list of buttons, respectively, should select the last or first item,
    /// also respectively.
    /// </summary>
    [Export] public bool Wrap = true;

    /// <summary>sWhich item to default focusing on when grabbing focus with nothing specified.</summary>
    [Export] public int DefaultFocus = 0;

    public ContextMenu() : base() { _cache = new(this); }

    /// <inheritdoc cref="Control.GrabFocus"/>
    /// <param name="option">Option corresponding to the button to focus on.</param>
    public void GrabFocus(StringName option) => _items[option].GrabFocus();

    /// <inheritdoc cref="Control.GrabFocus"/>
    /// <param name="index">Index of the button to focus on.</param>
    public void GrabFocus(int index) => _items[_options[index]].GrabFocus();

    /// <inheritdoc cref="Control.GrabFocus"/>
    /// <remarks>Grabs focus of the button at index <see cref="DefaultFocus"/></remarks>
    public new void GrabFocus() => GrabFocus(DefaultFocus);

    public void OnInputModeChanged(InputMode mode)
    {
        switch (mode)
        {
        case InputMode.Mouse:
            foreach ((var _, Button item) in _items)
                item.MouseFilter = MouseFilterEnum.Stop;
            if (_selected != NothingSelected)
            {
                Input.WarpMouse(_items[_options[_selected]].GlobalPosition + _items[_options[_selected]].Size/2);
                _items[_options[_selected]].ReleaseFocus();
            }
            break;
        default:
            _focus = _hovered == NothingSelected ? (_selected == NothingSelected ? 0 : _selected) : _hovered;
            foreach ((var _, Button item) in _items)
                item.MouseFilter = MouseFilterEnum.Ignore;
            if (Input.IsActionPressed(InputManager.UiAccept))
            {
                GrabFocus(_focus);
                _focus = NothingSelected;
                // Prevent the focused button from being pressed at the same time as being focused on
                GetViewport().SetInputAsHandled();
            }
            break;
        }
    }

    /// <summary>
    /// If a focus target was assigned due to switching input mode, focus on that. Otherwise, if nothing is focused, focus on the last-focused item.
    /// Otherwise, move focus up or down one item depending on the direction pressed.
    /// </summary>
    /// <param name="direction">Direction to move focus.</param>
    public void OnDirectionPressed(Vector2I direction)
    {
        if (_focus != NothingSelected)
        {
            GrabFocus(_focus);
            _focus = NothingSelected;
        }
        else
        {
            int next = _selected == NothingSelected ? 0 : _selected;
            if (_selected != NothingSelected && _items[_options[_selected]].HasFocus())
                next = Wrap ? (_selected + direction.Y + _options.Length) % _options.Length : Mathf.Clamp(_selected + direction.Y, 0, _options.Length - 1);
            GrabFocus(next);
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        DeviceManager.Singleton.InputModeChanged += OnInputModeChanged;
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            UpdateItems();
            for (int i = 0; i < _options.Length; i++)
            {
                int index = i;
                _items[_options[index]].MouseFilter = DeviceManager.Mode == InputMode.Mouse ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
                _items[_options[index]].FocusEntered += () => {
                    _selected = index;
                    if (DeviceManager.Mode != InputMode.Mouse)
                        HighlightSound.Play();
                };
                _items[_options[index]].Pressed += () => {
                    EmitSignal(SignalName.ItemSelected, _options[index]);
                    QueueFree();
                    EmitSignal(SignalName.MenuClosed);
                };

                _items[_options[index]].MouseEntered += () => {
                    _hovered = index;
                    HighlightSound.Play();
                };
                _items[_options[index]].MouseExited += () => _hovered = -1;
            }            
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputManager.UiHome))
        {
            GrabFocus(0);
            GetViewport().SetInputAsHandled();
        }
        if (@event.IsActionPressed(InputManager.UiEnd))
        {
            GrabFocus(_options.Length - 1);
            GetViewport().SetInputAsHandled();
        }

        if (@event.IsActionPressed(InputManager.Cancel))
        {
            EmitSignal(SignalName.MenuCanceled);
            GetViewport().SetInputAsHandled();
            QueueFree();
            EmitSignal(SignalName.MenuClosed);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DeviceManager.Singleton.InputModeChanged -= OnInputModeChanged;
    }
}