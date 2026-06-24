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

internal static class GlobalConfigStore
{
    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VS.Helper",
        "global.config.json");

    public static GlobalConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new GlobalConfig();

            return JsonSerializer.Deserialize<GlobalConfig>(File.ReadAllText(ConfigPath)) ?? new GlobalConfig();
        }
        catch
        {
            return new GlobalConfig();
        }
    }

    public static void Save(GlobalConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}
