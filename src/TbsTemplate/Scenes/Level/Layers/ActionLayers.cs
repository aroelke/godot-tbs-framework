using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Godot;
using TbsTemplate.Scenes.Level.Map;

namespace TbsTemplate.Scenes.Level.Layers;

/// <summary>
/// A <see cref="Grid"/> overlay comprised of a list of <see cref="TileMapLayer"/>s useful for displaying information about cells. Layers lower in the scene tree
/// (and therefore drawn later) can optionally be made to only show cells not also included in higher layers.
/// </summary>
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

    /// <summary>Whether or not to display layers on top of each other or for earlier-drawn layers to overwrite later-drawn ones.</summary>
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

    /// <summary>Get or set the set of cells highlighted in a particular layer.</summary>
    /// <param name="name">Name of the layer to modify.</param>
    public IEnumerable<Vector2I> this[StringName name]
    {
        get => _cells[name];
        set
        {
            if (_cells[name] != value)
            {
                _cells[name] = value.ToImmutableHashSet();
                UpdateLayers();
            }
        }
    }

    /// <returns>The unique set of cells occupied by all layers.</returns>
    public ImmutableHashSet<Vector2I> Union() => _cells.Select((p) => p.Value).Aggregate((a, b) => a.Union(b));

    /// <summary>Clear a specific layer.</summary>
    public void Clear(StringName layer)
    {
        _layers[layer].Clear();
        _cells[layer] = [];
        UpdateLayers();
    }

    /// <summary>Clear all layers.</summary>
    public void Clear()
    {
        foreach ((StringName name, _) in _layers)
            Clear(name);
    }

    /// <summary>Clear all but one layer.</summary>
    /// <param name="name">Name of the layer to keep.</param>
    public void Keep(StringName name)
    {
        foreach ((StringName layer, _) in _layers)
            if (layer != name)
                Clear(layer);
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
