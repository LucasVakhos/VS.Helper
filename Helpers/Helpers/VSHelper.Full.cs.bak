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

#region ========================= GLOBAL CONFIG =========================

internal class GlobalConfig
{
    public string AccessKey { get; set; } = "";
    public string DefaultBrowser { get; set; } = "";
    public bool AiRouterEnabled { get; set; } = true;
    public bool SmartRoutingEnabled { get; set; } = true;
    public string Version { get; set; } = "1.0.0";
}

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

#endregion

#region ========================= AI ROUTER =========================

internal enum RouteTarget
{
    Browser,
    Clipboard,
    Explorer,
    VisualStudio,
    Silent
}

internal class RouteIntent
{
    public RouteTarget Target { get; set; }
    public string Payload { get; set; }
    public string Title { get; set; }
}

internal static class AIRouter
{
    public static async Task ExecuteAsync(RouteIntent intent)
    {
        var cfg = GlobalConfigStore.Load();

        if (!cfg.AiRouterEnabled)
        {
            Clipboard.SetText(intent.Payload ?? "");
            return;
        }

        switch (intent.Target)
        {
            case RouteTarget.Browser:
                await BrowserKernel.OpenAsync(intent.Payload, cfg);
                break;

            case RouteTarget.Clipboard:
                Clipboard.SetText(intent.Payload ?? "");
                break;

            case RouteTarget.Explorer:
                Process.Start("explorer.exe", intent.Payload);
                break;

            case RouteTarget.Silent:
                return;
        }
    }
}

#endregion

#region ========================= BROWSER KERNEL =========================

internal static class BrowserKernel
{
    public static Task OpenAsync(string file, GlobalConfig cfg)
    {
        var proc = Process.GetProcesses()
            .FirstOrDefault(p =>
                p.ProcessName.Contains("chrome") ||
                p.ProcessName.Contains("msedge") ||
                p.ProcessName.Contains("firefox"));

        if (proc != null)
        {
            SetForegroundWindow(proc.MainWindowHandle);
            return Task.CompletedTask;
        }

        var browser = cfg.DefaultBrowser;

        if (string.IsNullOrWhiteSpace(browser) || !File.Exists(browser))
        {
            browser = SelectBrowser();
            cfg.DefaultBrowser = browser;
            GlobalConfigStore.Save(cfg);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = browser,
            Arguments = $"\"{file}\"",
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }

    private static string SelectBrowser()
    {
        using var dlg = new OpenFileDialog();
        dlg.Filter = "Browser|chrome.exe;msedge.exe;firefox.exe";

        return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : "";
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}

#endregion

#region ========================= CREATE ZIP COMMAND =========================

internal sealed class CreateZipCommand
{
    public static async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        if (dte?.Solution == null) return;

        var sln = dte.Solution.FullName;
        var dir = Path.GetDirectoryName(sln)!;
        var name = Path.GetFileNameWithoutExtension(sln);

        var zip = Path.Combine(dir, $"{name}.zip");

        if (File.Exists(zip))
            File.Delete(zip);

        ZipFile.CreateFromDirectory(dir, zip, CompressionLevel.Optimal, false);

        var data = new DataObject();
        var files = new StringCollection { zip };
        data.SetFileDropList(files);
        Clipboard.SetDataObject(data, true);

        await AIRouter.ExecuteAsync(new RouteIntent
        {
            Target = RouteTarget.Browser,
            Payload = zip,
            Title = "Open ZIP"
        });
    }
}

#endregion

#region ========================= BUILD SOLUTION =========================

internal sealed class BuildSolutionCommand
{
    public static async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        if (dte?.Solution == null) return;

        foreach (EnvDTE.Project project in dte.Solution.Projects)
        {
            try
            {
                project.Properties?.Item("Version")?.Value?.ToString();
            }
            catch { }
        }

        dte.Solution.SolutionBuild.Build(true);
    }
}

#endregion

#region ========================= CONFIG DASHBOARD =========================

internal class ConfigViewModel : INotifyPropertyChanged
{
    private GlobalConfig cfg = GlobalConfigStore.Load();

    public string AccessKey
    {
        get => cfg.AccessKey;
        set { cfg.AccessKey = value; Save(); }
    }

    public string DefaultBrowser
    {
        get => cfg.DefaultBrowser;
        set { cfg.DefaultBrowser = value; Save(); }
    }

    public bool AiRouterEnabled
    {
        get => cfg.AiRouterEnabled;
        set { cfg.AiRouterEnabled = value; Save(); }
    }

    public bool SmartRoutingEnabled
    {
        get => cfg.SmartRoutingEnabled;
        set { cfg.SmartRoutingEnabled = value; Save(); }
    }

    private void Save()
    {
        GlobalConfigStore.Save(cfg);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}

#endregion