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

internal static class SelfUpgradeCore
{
    public static async Task RunAsync(DTE2 dte)
    {
        if (!await ProjectScopeGuard.IsVSHelperSolutionAsync(dte))
            return;

        string solutionPath = await DTEProxy.GetSolutionPathAsync(dte);
        string solutionDir = Path.GetDirectoryName(solutionPath)!;

        string version = VersionBumpEngine.Bump(solutionDir);
        await DTEProxy.SetStatusAsync(dte, "VS.Helper version bumped: " + version);
        await DTEProxy.BuildSolutionAsync(dte);

        string vsix = Directory.GetFiles(solutionDir, "*.vsix", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(vsix))
        {
            SysProcess.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = vsix,
                UseShellExecute = true
            });
        }
    }
}
