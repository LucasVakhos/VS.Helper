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

internal static class DefenseResponse
{
    public static void SafeModeActivate()
    {
        var cfg = GlobalConfigStore.Load();
        cfg.AiRouterEnabled = false;
        cfg.SmartRoutingEnabled = false;
        GlobalConfigStore.Save(cfg);
    }
}
