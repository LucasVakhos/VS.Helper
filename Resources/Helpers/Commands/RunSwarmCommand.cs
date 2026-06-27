using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using VS.Helper.AI;

namespace VS.Helper.Commands;

[Command(PackageIds.RunSwarmCommand)]
internal sealed class RunSwarmCommand : BaseCommand<RunSwarmCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Command.Enabled = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is DTE2 dte
            && dte.Solution != null
            && !string.IsNullOrWhiteSpace(dte.Solution.FullName);
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is DTE2 dte)
            await AgentSwarmCore.RunAsync(dte);
    }
}
