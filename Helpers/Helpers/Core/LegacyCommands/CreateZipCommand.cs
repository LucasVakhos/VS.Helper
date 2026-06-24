// Core\LegacyCommands\CreateZipCommand.cs
// Compatibility shim: old merged command name now delegates to the new ZIP engine.
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Threading.Tasks;
using VS.Helper.Core.Zip;

namespace VS.Helper;

internal sealed class CreateZipCommand
{
    public static async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        string solutionPath = dte?.Solution == null ? null : dte.Solution.FullName;

        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return;

        new ZipBuildEngine().Build(solutionPath);
    }
}
