// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
namespace VS.Helper.AI;

internal sealed class SwarmError
{
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }

    public string Fingerprint => SwarmHash.Normalize(Description);
}
