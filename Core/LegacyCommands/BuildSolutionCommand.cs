// <auto-split from VSHelper.Full.cs>
using EnvDTE;
using EnvDTE80;
using System;
using System.Linq;
namespace VS.Helper;

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
