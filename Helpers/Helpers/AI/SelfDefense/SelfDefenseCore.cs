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

internal static class SelfDefenseCore
{
    public static bool CheckSystem()
    {
        return CheckConfig() && CheckRuntimeState();
    }

    private static bool CheckConfig()
    {
        try
        {
            return GlobalConfigStore.Load() != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckRuntimeState()
    {
        return AppDomain.CurrentDomain.FriendlyName.IndexOf("devenv", StringComparison.OrdinalIgnoreCase) >= 0
            || AppDomain.CurrentDomain.FriendlyName.IndexOf("DefaultDomain", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
