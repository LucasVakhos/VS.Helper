// <auto-split from VSHelper.AgentSwarm.Full.cs>
using EnvDTE80;
using System;
using System.Linq;
namespace VS.Helper.AI;

internal static class AgentSwarmCore
{
    private const int MaxPasses = 3;

    public static async Task RunAsync(DTE2 dte)
    {
        if (!SelfDefenseCore.CheckSystem())
        {
            DefenseResponse.SafeModeActivate();
            return;
        }

        for (int pass = 1; pass <= MaxPasses; pass++)
        {
            var errors = await DTEProxy.GetErrorsAsync(dte);
            if (errors.Count == 0)
            {
                await DTEProxy.SetStatusAsync(dte, "SWARM STABLE ✔");
                return;
            }

            foreach (var error in errors)
            {
                string strategy = SwarmRuleEngine.PickStrategy(error);
                bool success = await FixAgent.ApplyAsync(strategy, dte);
                SwarmMemoryStore.Record(error.Fingerprint, error.Description, strategy, success);
            }

            await OptimizationAgent.RunAsync(dte);
            await QualityAgent.CheckAsync(dte);
            await DTEProxy.RebuildSolutionAsync(dte);
            await Task.Delay(1500);
        }

        await DTEProxy.SetStatusAsync(dte, "SWARM SAFE STOP: max passes reached");
    }
}
