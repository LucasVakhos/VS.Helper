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

internal static class FixAgent
{
    public static async Task<bool> ApplyAsync(string strategy, DTE2 dte)
    {
        try
        {
            switch (strategy)
            {
                case SwarmStrategies.AddAlias:
                    await DTEProxy.InsertAtTopOfActiveDocumentAsync(dte,
                        "using SysProcess = System.Diagnostics.Process;\nusing DTEProject = EnvDTE.Project;");
                    return true;

                case SwarmStrategies.ThreadOrchestratorHint:
                    await DTEProxy.SetStatusAsync(dte,
                        "VSTHRD109: replace ThrowIfNotOnUIThread with SwitchToMainThreadAsync / DTEProxy.");
                    return true;

                case SwarmStrategies.SafeMode:
                    DefenseResponse.SafeModeActivate();
                    return true;

                case SwarmStrategies.Rebuild:
                default:
                    await DTEProxy.RebuildSolutionAsync(dte);
                    return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
