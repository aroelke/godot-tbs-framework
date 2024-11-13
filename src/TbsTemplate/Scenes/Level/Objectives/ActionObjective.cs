using Godot;
using TbsTemplate.Scenes.Level.Layers;
using TbsTemplate.Scenes.Level.Object;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>Objective that's completed upon performing a special action in a region.</summary>
[Tool]
public partial class ActionObjective : Objective
{
    private int _completed = 0;

    /// <summary>Region to perform the action in.  Also defines which units can perform the action. Side effects are not implemented here.</summary>
    [Export] public SpecialActionRegion ActionRegion = null;

    /// <summary>Number of units in the allowed list that need to perform the action.</summary>
    [Export(PropertyHint.Range, "1,10,or_greater")] public int Target = 1;

    public override bool Complete => _completed >= Target;

    public override string Description => ActionRegion is null ? "" : $"{ActionRegion.Action} in {ActionRegion.Name} with {Target} allowed units";

    public void ActionPerformed(StringName action, Unit actor, Vector2I cell) => _completed++;

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint() && ActionRegion is not null)
            ActionRegion.SpecialActionPerformed += ActionPerformed;
    }
}