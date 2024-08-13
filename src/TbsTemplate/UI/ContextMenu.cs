using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TbsTemplate.UI;

[SceneTree, Tool]
public partial class ContextMenu : NinePatchRect
{
    [Signal] public delegate void ItemSelectedEventHandler(StringName item);

    private StringName[] _options = [];
    private readonly Dictionary<StringName, Button> _items = [];

    public Button this[StringName option] => _items[option];

    [Export] public StringName[] Options
    {
        get => _options;
        private set
        {
            if (Engine.IsEditorHint() && _options != value)
            {
                _options = value;
                _items.Clear();

                foreach (Button child in Items.GetChildren().OfType<Button>())
                    child.QueueFree();
                foreach (StringName option in _options)
                    Items.AddChild(_items[option] = new() { Text = option });
            }
        }
    }

    [Export] public bool Wrap = true;

    [OnInstantiate]
    public void Initialize(IOrderedEnumerable<StringName> options)
    {
        Options = [.. options];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            for (int i = 0; i < _options.Length; i++)
            {
                _items[_options[i]].Pressed += () => EmitSignal(SignalName.ItemSelected, _options[i]);

                _items[_options[i]].FocusPrevious = GetPathTo(_items[_options[(i - 1 + _options.Length) % _options.Length]], useUniquePath:true);
                _items[_options[i]].FocusNext = GetPathTo(_items[_options[(i + 1) % _options.Length]], useUniquePath:true);
                if (Wrap)
                {
                    _items[_options[i]].FocusNeighborTop = _items[_options[i]].FocusPrevious;
                    _items[_options[i]].FocusNeighborBottom = _items[_options[i]].FocusNext;
                }
                else
                {
                    if (i > 0)
                        _items[_options[i]].FocusNeighborTop = GetPathTo(_items[_options[i - 1]], useUniquePath:true);
                    if (i < _options.Length - 1)
                        _items[_options[i]].FocusNeighborBottom = GetPathTo(_items[_options[i + 1]], useUniquePath:true);
                }
            }
        }
    }
}