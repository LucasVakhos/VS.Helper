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


