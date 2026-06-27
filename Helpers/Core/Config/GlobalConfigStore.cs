// <auto-split from VSHelper.Full.cs>
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Windows.Controls;
using Process = System.Diagnostics.Process;
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
