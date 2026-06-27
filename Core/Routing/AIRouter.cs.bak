// <auto-split from VSHelper.Full.cs>
using System;
using System.Linq;
using System.Windows.Forms;
using Process = System.Diagnostics.Process;
namespace VS.Helper;

internal static class AIRouter
{
    public static async Task ExecuteAsync(RouteIntent intent)
    {
        var cfg = GlobalConfigStore.Load();

        if (!cfg.AiRouterEnabled)
        {
            Clipboard.SetText(intent.Payload ?? string.Empty);
            return;
        }

        switch (intent.Target)
        {
            case RouteTarget.Browser:
                await BrowserKernel.OpenAsync(intent.Payload, cfg);
                break;

            case RouteTarget.Clipboard:
                Clipboard.SetText(intent.Payload ?? string.Empty);
                break;

            case RouteTarget.Explorer:
                Process.Start("explorer.exe", intent.Payload);
                break;

            case RouteTarget.Silent:
                return;
        }
    }
}


