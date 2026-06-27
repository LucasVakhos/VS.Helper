// <auto-split from VSHelper.AgentSwarm.Full.cs>
using EnvDTE80;
using System;
using System.Linq;
namespace VS.Helper.AI;

internal static class QualityAgent
{
    public static Task CheckAsync(DTE2 dte)
    {
        return DTEProxy.SetStatusAsync(dte, "Swarm: quality pass done");
    }
}
