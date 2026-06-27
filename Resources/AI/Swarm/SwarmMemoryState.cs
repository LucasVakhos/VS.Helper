// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Collections.Generic;
using System.Linq;
namespace VS.Helper.AI;

internal sealed class SwarmMemoryState
{
    public List<SwarmMemoryEntry> Entries { get; set; } = new();
}
