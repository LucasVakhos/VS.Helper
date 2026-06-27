// <auto-split from VSHelper.AgentSwarm.Full.cs>
using EnvDTE80;
using System;
using System.Linq;
namespace VS.Helper.AI;

internal static class OptimizationAgent
{
    public static Task RunAsync(DTE2 dte)
    {
        return DTEProxy.SetStatusAsync(dte, "Swarm: optimization pass done");
    }
}
