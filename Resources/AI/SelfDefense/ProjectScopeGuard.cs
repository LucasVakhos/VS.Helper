// <auto-split from VSHelper.AgentSwarm.Full.cs>
using EnvDTE80;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
