// <auto-split from VSHelper.AgentSwarm.Full.cs>
using EnvDTE80;
using System;
using System.Linq;
namespace VS.Helper.AI;

internal static class AgentSwarmEvolutionCore
{
    public static async Task RunAsync(DTE2 dte)
    {
        await ThreadOrchestrator.RunBackgroundAsync(async () =>
        {
            SwarmRuleEngine.EvolveFromMemory();
            await Task.CompletedTask;
        });

        await DTEProxy.SetStatusAsync(dte, "Swarm rules evolved ✔");
    }
}
