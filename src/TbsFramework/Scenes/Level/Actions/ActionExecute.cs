using Godot;
using TbsFramework.Scenes.Data;
using TbsFramework.Scenes.Level.Events;

namespace TbsFramework.Scenes.Level.Actions;

public record struct UnitActionExecuteResult(object Result, UnitData Actor, Vector2I Target, ActionExecute Action)
{
    public UnitActionExecuteResult(UnitActionResult action) : this(action.Result, action.Actor, action.Target, action.Action.ExecuteComponent) {}

    public readonly void UpdateGrid(GridData grid) => Action.UpdateGrid(grid, this);
}

[GlobalClass]
public abstract partial class ActionExecute : Resource
{
    /// <summary>
    /// Compute the result of performing this action.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="target"></param>
    /// <returns>A data structure representing the result of <paramref name="unit"/> performing this action on cell <paramref name="target"/>.</returns>
    /// <exception cref="ArgumentException">If <paramref name="target"/> is not a valid target cell to perform this action on.</exception>
    public abstract UnitActionExecuteResult Perform(UnitData unit, Vector2I target);

    /// <summary>
    /// Update a grid with the results of this action as computed by <see cref="Perform(UnitData, Vector2I)"/>.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="result"></param>
    /// <exception cref="ArgumentException">If <paramref name="result"/>.Result contains invalid data for performing this action.</exception>
    public abstract void UpdateGrid(GridData grid, UnitActionExecuteResult result);

    /// <summary>
    /// Simulate the results of this action, resolving any nondeterminism in some nonrandom way (such as by averaging possible results). Makes no changes
    /// to the state of any existing grid.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns>A new grid containing the result of the simulation of performing this action.</returns>
    /// <remarks><b>Note</b>: This is intended for use by <see cref="AIController"/> to evaluate actions.</remarks>
    public abstract GridData Simulate(UnitData unit, Vector2I source, Vector2I target);

    public virtual void Initialize(LevelManager manager) {}
}