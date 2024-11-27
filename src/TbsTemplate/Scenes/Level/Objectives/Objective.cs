using Godot;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>An objective that can be completed during the course of a level to signify if the level is won or lost.</summary>
[GlobalClass, Tool]
public abstract partial class Objective : Node
{
    /// <summary>Signals that the objective has switched status, either having been completed or uncompleted.</summary>
    /// <param name="complete">Whether or not the objective is complete.</param>
    [Signal] public delegate void StatusChangedEventHandler(bool complete);

    private bool _complete = false;

    /// <summary>
    /// <c>true</c> if the objective is currently complete, and <c>false</c> otherwise. Can change back and forth over time, and emits
    /// the StatusChanged signal any frame that it does.
    /// </summary>
    public abstract bool Complete { get; }

    /// <summary>Phrase describing the objective.</summary>
    public abstract string Description { get; }

    public override void _Ready()
    {
        base._Ready();
        if (!Engine.IsEditorHint())
            _complete = Complete;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Engine.IsEditorHint())
        {
            bool complete = Complete; // Ensures "Complete" is only evaluated once per process frame, since it's used thrice below
            if (complete != _complete)
                EmitSignal(SignalName.StatusChanged, complete);
            _complete = complete;
        }
    }
}