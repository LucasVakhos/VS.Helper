using EnvDTE;
using EnvDTE80;
using System;
using System.IO;
using System.Threading.Tasks;
using VS.Helper.AI;

namespace VS.Helper.Commands;

[Community.VisualStudio.Toolkit.Command(PackageIds.SelfUpgradeCommand)]
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
            string name = Path.GetFileNameWithoutExtension(dte.Solution.FullName);
            visible = string.Equals(name, "VS.Helper", StringComparison.OrdinalIgnoreCase);
        }

        Command.Enabled = visible;
        Command.Visible = visible;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is not DTE2 dte)
            return;

        string solutionPath = dte.Solution?.FullName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
        {
            dte.StatusBar.Text = "VS.Helper Self Upgrade: solution не найден.";
            return;
        }

        string name = Path.GetFileNameWithoutExtension(solutionPath);
        if (!string.Equals(name, "VS.Helper", StringComparison.OrdinalIgnoreCase))
        {
            dte.StatusBar.Text = "VS.Helper Self Upgrade: доступно только в solution VS.Helper.";
            return;
        }

        string solutionDir = Path.GetDirectoryName(solutionPath)!;

        // ВАЖНО: не вызываем DTE Build/SaveAll и не ждём внешние процессы.
        // Иначе команда может заблокировать UI-поток Visual Studio намертво.
        _ = Task.Run(() => SelfUpgradeCore.RunDetached(solutionDir, solutionPath));

        dte.StatusBar.Text = "VS.Helper Self Upgrade: install-only запущен. Закрой Visual Studio для установки.";
    }
}
