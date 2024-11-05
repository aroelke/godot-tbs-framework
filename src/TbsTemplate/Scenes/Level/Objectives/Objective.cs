using Godot;

namespace TbsTemplate.Scenes.Level.Objectives;

/// <summary>An objective that can be completed during the course of a level to signify if the level is complete or failed.</summary>
[GlobalClass, Tool]
public abstract partial class Objective : Node
{
    /// <summary>Signals that the objective has been accomplished.  Could fire multiple times if the objective becomes uncompleted and then accomplished again later.</summary>
    [Signal] public delegate void AccomplishedEventHandler();

    /// <summary>
    /// Signals that the objective has been uncompleted (i.e. it was completed and then became uncomplete again). Could fire multiple times if the objective becomes completed
    /// and then uncompleted later.
    /// </summary>
    [Signal] public delegate void RelinquishedEventHandler();

    private bool _completed = false;

    /// <summary>
    /// <c>true</c> if the objective is currently accomplished, and <c>false</c> otherwise. Setting <c>true</c> from <c>false</c> emits
    /// <see cref="SignalName.Accomplished"/>.
    /// </summary>
    public bool Completed
    {
        get => _completed;
        protected set
        {
            bool signal = (value && !_completed) || (!value && _completed);
            _completed = value;
            if (signal)
                EmitSignal(_completed? SignalName.Accomplished : SignalName.Relinquished);
        }
    }

    /// <summary>Phrase describing the objective.</summary>
    public abstract string Description { get; }
}