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

internal static class BrowserKernel
{
    public static Task OpenAsync(string file, GlobalConfig cfg)
    {
        var proc = Process.GetProcesses()
            .FirstOrDefault(p =>
                p.ProcessName.Contains("chrome") ||
                p.ProcessName.Contains("firefox") ||
                p.ProcessName.Contains("brave") ||
                p.ProcessName.Contains("opera"));

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
        dlg.Filter = "Browser|chrome.exe;firefox.exe;brave.exe;opera.exe";

        return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : "";
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}


