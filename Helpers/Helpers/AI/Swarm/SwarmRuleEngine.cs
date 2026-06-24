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

internal static class SwarmRuleEngine
{
    public static string PickStrategy(SwarmError error)
    {
        var state = SwarmRuleStore.Load();
        var memory = SwarmMemoryStore.GetOrCreate(error.Fingerprint, error.Description);

        var candidates = state.Rules
            .Where(x => x.Enabled)
            .Where(x => string.Equals(x.ErrorPattern, "default", StringComparison.OrdinalIgnoreCase)
                || error.Description.IndexOf(x.ErrorPattern, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderByDescending(x => x.SuccessScore - x.FailCount)
            .ToList();

        if (memory.FailCount >= 3)
        {
            var nonLast = candidates.FirstOrDefault(x => !string.Equals(x.Strategy, memory.LastStrategy, StringComparison.OrdinalIgnoreCase));
            if (nonLast != null)
                return nonLast.Strategy;
        }

        return candidates.FirstOrDefault()?.Strategy ?? SwarmStrategies.Rebuild;
    }

    public static void EvolveFromMemory()
    {
        var rules = SwarmRuleStore.Load();
        var memory = SwarmMemoryStore.Load();

        foreach (var item in memory.Entries)
        {
            if (string.IsNullOrWhiteSpace(item.LastStrategy))
                continue;

            var rule = rules.Rules.FirstOrDefault(x =>
                string.Equals(x.ErrorPattern, item.Fingerprint, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Strategy, item.LastStrategy, StringComparison.OrdinalIgnoreCase));

            if (rule == null)
            {
                rule = new SwarmRule
                {
                    ErrorPattern = item.Fingerprint,
                    Strategy = item.LastStrategy,
                    SuccessScore = item.SuccessCount,
                    FailCount = item.FailCount,
                    Enabled = item.FailCount < 5
                };
                rules.Rules.Add(rule);
            }
            else
            {
                rule.SuccessScore = item.SuccessCount;
                rule.FailCount = item.FailCount;
                rule.Enabled = item.FailCount < 5 || item.SuccessCount > item.FailCount;
                rule.LastUsedUtc = DateTime.UtcNow;
            }
        }

        SwarmRuleStore.Save(rules);
    }
}
