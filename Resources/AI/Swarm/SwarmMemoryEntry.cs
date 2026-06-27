// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
namespace VS.Helper.AI;

internal sealed class SwarmMemoryEntry
{
    public string Fingerprint { get; set; } = string.Empty;
    public string LastError { get; set; } = string.Empty;
    public string LastStrategy { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}
