using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace TbsTemplate.Scenes.Level.Layers;

[Tool]
public partial class ActionLayers : Node2D
{
    private bool _union = false;
    private readonly Dictionary<StringName, TileMapLayer> _layers = [];
    private readonly Dictionary<StringName, ImmutableHashSet<Vector2I>> _cells = [];

    private void UpdateLayers()
    {
        foreach ((_, TileMapLayer layer) in _layers)
            layer.Clear();
        if (ShowUnion)
        {
            foreach ((StringName name, ImmutableHashSet<Vector2I> cells) in _cells)
                _layers[name].SetCellsTerrainConnect(new(cells), 0, 0);
        }
        else
        {
            for (int i = 0; i < GetChildCount(); i++)
            {
                ImmutableHashSet<Vector2I> used = _cells[GetChild(i).Name];
                for (int j = 0; j < i; j++)
                    used = used.Except(_cells[GetChild(j).Name]);
                _layers[GetChild(i).Name].SetCellsTerrainConnect(new(used), 0, 0);
            }
        }
    }

    [Export] public bool ShowUnion
    {
        get => _union;
        set
        {
            if (_union != value)
            {
                _union = value;
                if (!Engine.IsEditorHint())
                    UpdateLayers();
            }
        }
    }

    public StringName this[int i]
    {
        get => GetChild(i).Name;
        set
        {
            foreach (Node child in GetChildren())
                if (child.Name == value)
                    MoveChild(child, i);
            UpdateLayers();
        }
    }

    public ImmutableHashSet<Vector2I> this[StringName name]
    {
        get => _cells[name];
        set
        {
            if (_cells[name] != value)
            {
                _cells[name] = value;
                UpdateLayers();
            }
        }
    }

    public ImmutableHashSet<Vector2I> Union() => _cells.Select((p) => p.Value).Aggregate((a, b) => a.Union(b));

    public void Clear()
    {
        foreach (TileMapLayer layer in GetChildren().OfType<TileMapLayer>())
            layer.Clear();
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? []);

        for (int i = 0; i < GetChildCount(); i++)
        {
            if (GetChildOrNull<TileMapLayer>(i) is null)
                warnings.Add($"Node {GetChild(i).Name} is not a TileMapLayer.");
        }

        return [.. warnings];
    }

    public override void _Ready()
    {
        base._Ready();

        if (!Engine.IsEditorHint())
        {
            foreach (TileMapLayer layer in GetChildren().OfType<TileMapLayer>())
            {
                _layers[layer.Name] = layer;
                _cells[layer.Name] = [.. layer.GetUsedCells()];
            }
        }
    }
}
