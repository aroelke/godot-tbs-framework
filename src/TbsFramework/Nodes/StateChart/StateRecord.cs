using System.Collections.Generic;
using TbsFramework.Nodes.StateCharts.States;

namespace TbsFramework.Nodes.StateCharts;

/// <summary>
/// Recursive mapping of a <see cref="State"/>'s active child <see cref="State"/>(s) onto their respective mappings. Used for saving and restoring states
/// with <see cref="HistoryState"/>.
/// </summary>
public class StateRecord
{
    /// <summary>The corresponding <see cref="State"/>'s active child <see cref="State"/>(s) mapped to their records.</summary>
    public Dictionary<State, StateRecord> Active;
}