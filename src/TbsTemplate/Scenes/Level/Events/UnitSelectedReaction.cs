using Godot;
using TbsTemplate.Nodes.StateChart.States;
using TbsTemplate.Nodes.StateChart.Reactions;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Events;

/// <summary>Allows a <see cref="State"/> to react to units being selected.</summary>
public partial class UnitSelectedReaction : Reaction, IReaction<Unit>
{
    /// <summary>Signals that a unit has been selected while the parent state is active.</summary>
    /// <param name="unit">Unit that was selected.</param>
    [Signal] public delegate void StateUnitSelectedEventHandler(Unit unit);

    public void React(Unit unit) => EmitSignal(SignalName.StateUnitSelected, unit);

    public void OnUnitSelected(Unit unit)
    {
        if (Active)
            React(unit);
    }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            LevelEvents.Singleton.UnitSelected += OnUnitSelected;
    }
}