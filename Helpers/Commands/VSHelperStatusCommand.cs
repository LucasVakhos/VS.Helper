// Commands\VSHelperStatusCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VS.Helper.Commands;

[Command(PackageIds.VSHelperStatusCommand)]
internal sealed class VSHelperStatusCommand : BaseCommand<VSHelperStatusCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Command.Visible = true;
        Command.Enabled = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        string solution = "(solution не открыт)";
        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is DTE dte &&
            dte.Solution != null &&
            !string.IsNullOrWhiteSpace(dte.Solution.FullName))
        {
            solution = dte.Solution.FullName;
        }

        MessageBox.Show(
            "VS.Helper активен.\n\n" +
            "Root menu mode: ON\n" +
            "Build Zip engine: NEW PIPELINE\n\n" +
            "Solution:\n" + solution,
            "VS.Helper",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
