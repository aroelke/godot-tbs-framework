using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Level.Object;
using Level.UI;

namespace Level.Map;

/// <summary>Contains several sets of <see cref="Grid"/> cells that represent where a <see cref="Unit"/> could move to, attack, and support.</summary>
public readonly struct ActionRanges
{
    /// <summary>Set of cells that can be moved to.</summary>
    public readonly ImmutableHashSet<Vector2I> Traversable;
    /// <summary>Set of cells that could be targeted for attack.</summary>
    public readonly ImmutableHashSet<Vector2I> Attackable;
    /// <summary>Set of cells that could be targeted for support.</summary>
    public readonly ImmutableHashSet<Vector2I> Supportable;

    /// <summary>Create a new set of action ranges out of the given sets of cells.</summary>
    public ActionRanges(IEnumerable<Vector2I> traversable, IEnumerable<Vector2I> attackable, IEnumerable<Vector2I> supportable)
    {
        Traversable = traversable.ToImmutableHashSet();
        Attackable  = attackable.ToImmutableHashSet();
        Supportable = supportable.ToImmutableHashSet();
    }

    /// <summary>Create a new set of action ranges with no traversable cells.</summary>
    /// <param name="attackable">Set of cells that could be attacked.</param>
    /// <param name="supportable">Set of cells that could be supported.</param>
    public ActionRanges(IEnumerable<Vector2I> attackable, IEnumerable<Vector2I> supportable) : this(ImmutableHashSet<Vector2I>.Empty, attackable, supportable) {}

    /// <summary>Default constructor. Creates an empty set of actionable cells.</summary>
    public ActionRanges() : this(ImmutableHashSet<Vector2I>.Empty, ImmutableHashSet<Vector2I>.Empty, ImmutableHashSet<Vector2I>.Empty) {}

    /// <summary>Maps a <see cref="string"/> name of an action range onto a set of cells.</summary>
    /// <param name="range">Name of the range. Should be one of <see cref="RangeOverlay.Traversable"/>, <see cref="RangeOverlay.Attackable"/>, or <see cref="RangeOverlay.Supportable"/>.</param>
    /// <returns>The set of cells corresponding to the name.</returns>
    /// <exception cref="IndexOutOfRangeException">If the name of the range isn't one of <see cref="RangeOverlay.Traversable"/>, <see cref="RangeOverlay.Attackable"/>, or <see cref="RangeOverlay.Supportable"/>.</exception>
    public ImmutableHashSet<Vector2I> this[string range] => range switch
    {
        RangeOverlay.Traversable => Traversable,
        RangeOverlay.Attackable  => Attackable,
        RangeOverlay.Supportable => Supportable,
        _ => throw new IndexOutOfRangeException($"There is no action range named {range}")
    };

    /// <summary>
    /// Filters out the action ranges based on grid occupants.  <see cref="Traversable"/> and <see cref="Supportable"/> ranges are filtered to remove cells containing enemies and
    /// the <see cref="Attackable"/> range is filtered to remove cells containing allies.
    /// </summary>
    /// <param name="allies"></param>
    /// <param name="enemies"></param>
    /// <returns></returns>
    public ActionRanges WithOccupants(IEnumerable<Unit> allies, IEnumerable<Unit> enemies) => new(
        Traversable.Where((c) => !enemies.Any((u) => u.Cell == c)),
        Attackable.Where((c) => !allies.Any((u) => u.Cell == c)),
        Supportable.Where((c) => !enemies.Any((u) => u.Cell == c))
    );

    /// <summary>
    /// Convert the sets of action ranges into ones that are mutually exclusive, using a list of range names to prioritize. Ranges further down the list will be filtered out so they
    /// don't contain cells in any range above it in the list.
    /// </summary>
    /// <param name="priority">List of range names to use for filtering.</param>
    /// <returns>A new set of action ranges whose ranges are mutually exclusive.</returns>
    public ActionRanges Exclusive(string[] priority)
    {
        ActionRanges self = this;
        return new(
            this[priority[0]],
            this[priority[1]].Where((c) => !self[priority[0]].Contains(c)),
            this[priority[2]].Where((c) => !self[priority[0]].Contains(c) && !self[priority[1]].Contains(c))
        );
    }
    public ActionRanges Exclusive() => Exclusive(new[] { RangeOverlay.Traversable, RangeOverlay.Attackable, RangeOverlay.Supportable });

    /// <returns>A copy of this set of action ranges with empty sets of cells.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ActionRanges Clear() => new();
}