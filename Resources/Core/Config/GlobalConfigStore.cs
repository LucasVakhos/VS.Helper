// <auto-split from VSHelper.Full.cs>
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace VS.Helper;

internal static class GlobalConfigStore
{
    private static string PathFile =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VS.Helper",
            "global.config.json");

    public static GlobalConfig Load()
    {
        if (!File.Exists(PathFile))
            return new GlobalConfig();

        return JsonSerializer.Deserialize<GlobalConfig>(File.ReadAllText(PathFile))
               ?? new GlobalConfig();
    }

    public static void Save(GlobalConfig cfg)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathFile)!);

        File.WriteAllText(PathFile,
            JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }
}


