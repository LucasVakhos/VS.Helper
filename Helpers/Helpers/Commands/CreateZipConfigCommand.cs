// Commands\CreateZipConfigCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VS.Helper.Core.Zip;

namespace VS.Helper.Commands;

[Command(PackageIds.CreateZipConfigCommand)]
internal sealed class CreateZipConfigCommand : BaseCommand<CreateZipConfigCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Command.Enabled = TryGetSolutionPath(out _);
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (!TryGetSolutionPath(out string solutionPath))
        {
            ShowInfo("Нет открытого Solution.");
            return;
        }

        try
        {
            ZipBuildConfig config = ZipBuildConfig.LoadOrCreateDefault(solutionPath);
            EnsureSolutionFoldersInConfig(solutionPath, config);

            string configPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, ZipBuildConfig.FileName);
            using ZipConfigEditorDialog dialog = new(solutionPath, config);
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Retry)
                OpenConfigForEdit(configPath);
            else if (result == System.Windows.Forms.DialogResult.OK)
                ShowInfo("ZIP config сохранён:\r\n" + configPath);
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
        }
    }

    private static void OpenConfigForEdit(string configPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            return;

        try
        {
            if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is DTE2 dte)
            {
                Window window = dte.ItemOperations.OpenFile(configPath, EnvDTE.Constants.vsViewKindTextView);
                window?.Activate();
                return;
            }
        }
        catch
        {
            // fallback ниже
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Если даже fallback не сработал, сообщение всё равно покажет путь.
        }
    }

    private static bool TryGetSolutionPath(out string solutionPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        solutionPath = null;

        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is not DTE2 dte)
            return false;

        solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "VS.Helper", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "Ошибка ZIP-конфига", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
    }
}
