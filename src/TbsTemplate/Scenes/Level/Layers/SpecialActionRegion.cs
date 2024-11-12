using System.Linq;
using Godot;
using TbsTemplate.Scenes.Level.Object;
using TbsTemplate.Scenes.Level.Object.Group;
using TbsTemplate.UI;

namespace TbsTemplate.Scenes.Level.Layers;

/// <summary>Map layer marking out a region where a unit can perform a special action such as capture or escape.</summary>
[GlobalClass, Tool]
public partial class SpecialActionRegion : TileMapLayer
{
    /// <summary>Signifies that the action corresponding to the region has been performed.</summary>
    /// <param name="name">Name of the action being performed.</param>
    /// <param name="performer"><see cref="Unit"/> performing the action.</param>
    /// <param name="cell">Cell in which the unit performed the action.</param>
    [Signal] public delegate void SpecialActionPerformedEventHandler(StringName name, Unit performer, Vector2I cell);

    /// <summary>Short description of the action being performed for display in the UI (for example, in a <see cref="ContextMenu"/>).</summary>
    [Export] public StringName Action = "";

    /// <summary>List of armies whose units are allowed to perform the action.</summary>
    [Export] public Army[] AllowedArmies = [];

    /// <summary>List of individual units who are allowed to perform the action.</summary>
    [Export] public Unit[] AllowedUnits = [];

    /// <summary>Check if a unit can perform the special action in a cell.</summary>
    /// <returns><c>true</c> if <paramref name="unit"/> is allowed to perform the action and <paramref name="cell"/> is in the region, and <c>false</c> otherwise.</returns>
    public virtual bool HasSpecialAction(Unit unit, Vector2I cell) => (AllowedUnits.Contains(unit) || AllowedArmies.Any((a) => a.Faction.AlliedTo(unit))) && GetUsedCells().Contains(cell);

    /// <summary>Perform the special action. By default, this just emits a signal indicating the action is performed by a unit at a cell.</summary>
    /// <param name="performer">Unit performing the action.</param>
    /// <param name="cell">Cell the action is being performed in.</param>
    public virtual void PerformSpecialAction(Unit performer, Vector2I cell) => EmitSignal(SignalName.SpecialActionPerformed, Action, performer, cell);
}