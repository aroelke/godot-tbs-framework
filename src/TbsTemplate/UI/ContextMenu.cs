using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.UI;

[SceneTree, Tool]
public partial class ContextMenu : PanelContainer
{
    [Signal] public delegate void ItemSelectedEventHandler(StringName item);

    private StringName[] _options = [];
    private readonly Dictionary<StringName, Button> _items = [];

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

    [Export] public bool Wrap = true;

    [Export] public int DefaultFocus = 0;

    [OnInstantiate]
    public void Initialize(IEnumerable<StringName> options, bool wrap)
    {
        Options = [.. options];
        Wrap = wrap;
    }

    /// <inheritdoc cref="Control.GrabFocus"/>
    /// <param name="option">Option corresponding to the button to focus on.</param>
    public void GrabFocus(StringName option) => _items[option].GrabFocus();

    /// <inheritdoc cref="Control.GrabFocus"/>
    /// <param name="index">Index of the button to focus on.</param>
    public void GrabFocus(int index) => _items[_options[index]].GrabFocus();

    /// <inheritdoc cref="Control.GrabFocus"/>
    /// <remarks>Grabs focus of the button at index <see cref="DefaultFocus"/></remarks>
    public new void GrabFocus() => GrabFocus(DefaultFocus);

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            UpdateItems();

            for (int i = 0; i < _options.Length; i++)
            {
                StringName option = _options[i];

                _items[option].Pressed += () => EmitSignal(SignalName.ItemSelected, option);

                _items[option].FocusPrevious = _items[_options[(i - 1 + _options.Length) % _options.Length]].GetPath();
                _items[option].FocusNext = _items[_options[(i + 1) % _options.Length]].GetPath();
                if (Wrap)
                {
                    _items[option].FocusNeighborTop = _items[option].FocusPrevious;
                    _items[option].FocusNeighborBottom = _items[option].FocusNext;
                }
                else
                {
                    if (i > 0)
                        _items[option].FocusNeighborTop = _items[_options[i - 1]].GetPath();
                    if (i < _options.Length - 1)
                        _items[option].FocusNeighborBottom = _items[_options[i + 1]].GetPath();
                }
            }
        }
    }
}