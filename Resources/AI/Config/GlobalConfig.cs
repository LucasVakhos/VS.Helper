// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Linq;
namespace VS.Helper.AI;

internal sealed class GlobalConfig
{
    public string AccessKey { get; set; } = string.Empty;
    public string DefaultBrowser { get; set; } = string.Empty;
    public bool AiRouterEnabled { get; set; } = true;
    public bool SmartRoutingEnabled { get; set; } = true;
    public string Version { get; set; } = "1.0.0";
}
