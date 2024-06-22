using System.Collections.Generic;
using Nodes.StateChart.States;

namespace Nodes.StateChart;

public class StateRecord
{
    public Dictionary<State, StateRecord> Active;
}