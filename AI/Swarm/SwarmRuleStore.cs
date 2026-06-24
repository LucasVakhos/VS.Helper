// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using SysProcess = System.Diagnostics.Process;
namespace VS.Helper.AI;

internal static class SwarmRuleStore
{
    private static string RulesPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VS.Helper",
        "swarm.rules.json");

    public static SwarmRuleState Load()
    {
        try
        {
            if (!File.Exists(RulesPath))
                return CreateDefault();

            var state = JsonSerializer.Deserialize<SwarmRuleState>(File.ReadAllText(RulesPath)) ?? CreateDefault();
            if (state.Rules.Count == 0)
                return CreateDefault();

            return state;
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static void Save(SwarmRuleState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RulesPath)!);
        File.WriteAllText(RulesPath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static SwarmRuleState CreateDefault()
    {
        var state = new SwarmRuleState();
        state.Rules.Add(new SwarmRule { ErrorPattern = "CS0104", Strategy = SwarmStrategies.AddAlias, SuccessScore = 10 });
        state.Rules.Add(new SwarmRule { ErrorPattern = "CS0246", Strategy = SwarmStrategies.Rebuild, SuccessScore = 5 });
        state.Rules.Add(new SwarmRule { ErrorPattern = "VSTHRD109", Strategy = SwarmStrategies.ThreadOrchestratorHint, SuccessScore = 8 });
        state.Rules.Add(new SwarmRule { ErrorPattern = "default", Strategy = SwarmStrategies.Rebuild, SuccessScore = 1 });
        Save(state);
        return state;
    }
}
