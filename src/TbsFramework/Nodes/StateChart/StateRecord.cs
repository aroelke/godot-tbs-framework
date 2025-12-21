using System.Collections.Generic;
using TbsTemplate.Nodes.StateCharts.States;

namespace TbsTemplate.Nodes.StateCharts;

/// <summary>
/// Recursive mapping of a <see cref="State"/>'s active child <see cref="State"/>(s) onto their respective mappings. Used for saving and restoring states
/// with <see cref="HistoryState"/>.
/// </summary>
public class StateRecord
{
    /// <summary>The corresponding <see cref="State"/>'s active child <see cref="State"/>(s) mapped to their records.</summary>
    public Dictionary<State, StateRecord> Active;
}