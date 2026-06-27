// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
namespace VS.Helper.AI;

internal sealed class SwarmRule
{
    public string ErrorPattern { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public int SuccessScore { get; set; }
    public int FailCount { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
}
