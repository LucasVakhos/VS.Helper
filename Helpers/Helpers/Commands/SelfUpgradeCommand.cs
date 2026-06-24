using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using VS.Helper.AI;

namespace VS.Helper.Commands;

[Command(PackageIds.SelfUpgradeCommand)]
internal sealed class SelfUpgradeCommand : BaseCommand<SelfUpgradeCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        bool visible = false;
        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is DTE2 dte
            && dte.Solution != null
            && !string.IsNullOrWhiteSpace(dte.Solution.FullName))
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(dte.Solution.FullName);
            visible = string.Equals(name, "VS.Helper", StringComparison.OrdinalIgnoreCase);
        }

        Command.Enabled = visible;
        Command.Visible = visible;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is DTE2 dte)
            await SelfUpgradeCore.RunAsync(dte);
    }
}
