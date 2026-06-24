// Core\LegacyCommands\CreateZipCommand.cs
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VS.Helper.Core.Zip;

namespace VS.Helper;

internal sealed class CreateZipCommand
{
    public static async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (Package.GetGlobalService(typeof(DTE)) is not DTE2 dte)
            return;

        string solutionPath = dte.Solution?.FullName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return;

        ZipBuildResult result = new ZipBuildService().Build(solutionPath);

        DataObject data = new();
        StringCollection files = new() { result.ZipPath };
        data.SetFileDropList(files);
        Clipboard.SetDataObject(data, true);

        await AIRouter.ExecuteAsync(new RouteIntent
        {
            Target = RouteTarget.Browser,
            Payload = result.ZipPath,
            Title = "Open ZIP"
        });
    }
}
