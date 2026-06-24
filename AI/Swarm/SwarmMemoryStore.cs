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

internal static class SwarmMemoryStore
{
    private static string MemoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VS.Helper",
        "swarm.memory.json");

    public static SwarmMemoryState Load()
    {
        try
        {
            if (!File.Exists(MemoryPath))
                return new SwarmMemoryState();

            return JsonSerializer.Deserialize<SwarmMemoryState>(File.ReadAllText(MemoryPath)) ?? new SwarmMemoryState();
        }
        catch
        {
            return new SwarmMemoryState();
        }
    }

    public static void Save(SwarmMemoryState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MemoryPath)!);
        File.WriteAllText(MemoryPath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static SwarmMemoryEntry GetOrCreate(string fingerprint, string lastError)
    {
        var state = Load();
        var entry = state.Entries.FirstOrDefault(x => string.Equals(x.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            entry = new SwarmMemoryEntry { Fingerprint = fingerprint, LastError = lastError };
            state.Entries.Add(entry);
        }

        entry.LastSeenUtc = DateTime.UtcNow;
        Save(state);
        return entry;
    }

    public static void Record(string fingerprint, string error, string strategy, bool success)
    {
        var state = Load();
        var entry = state.Entries.FirstOrDefault(x => string.Equals(x.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            entry = new SwarmMemoryEntry { Fingerprint = fingerprint };
            state.Entries.Add(entry);
        }

        entry.LastError = error;
        entry.LastStrategy = strategy;
        entry.LastSeenUtc = DateTime.UtcNow;
        if (success)
            entry.SuccessCount++;
        else
            entry.FailCount++;

        Save(state);
    }
}
