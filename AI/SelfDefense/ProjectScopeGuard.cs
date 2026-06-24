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

internal static class ProjectScopeGuard
{
    public static async Task<bool> IsVSHelperSolutionAsync(DTE2 dte)
    {
        string solutionPath = await DTEProxy.GetSolutionPathAsync(dte);
        string name = Path.GetFileNameWithoutExtension(solutionPath);
        return string.Equals(name, "VS.Helper", StringComparison.OrdinalIgnoreCase);
    }
}
