// Commands\CreateZipConfigCommand.cs
using EnvDTE;
using System;
using System.IO;
using VS.Helper.Core.Zip;

namespace VS.Helper.Commands;

[Command(PackageIds.CreateZipConfigCommand)]
internal sealed class CreateZipConfigCommand : BaseCommand<CreateZipConfigCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Command.Enabled = HasOpenSolution();
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is not DTE dte)
        {
            Show("Не удалось получить DTE.", true);
            return;
        }

        string solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
        {
            Show("Нет открытого Solution.", true);
            return;
        }

        try
        {
            ZipBuildConfig config = ZipBuildConfig.CreateDefault(solutionPath);
            config.Save(solutionPath);

            string configPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, ZipBuildConfig.FileName);
            OpenConfigForEdit(dte, configPath);

        }
        catch (Exception ex)
        {
            Show(ex.ToString(), true);
        }
    }

    private static void OpenConfigForEdit(DTE dte, string configPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            return;

        try
        {
            Window window = dte.ItemOperations.OpenFile(configPath, EnvDTE.Constants.vsViewKindTextView);
            window?.Activate();
            return;
        }
        catch
        {
            // Fallback: open with default XML editor if VS editor could not open it.
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
            // Path is shown in the message box even if fallback failed.
        }
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is not DTE dte)
            return false;

        string solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static void Show(string message, bool error)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(
            message,
            error ? "Ошибка Create Zip Config" : "VS.Helper / Create Zip Config",
            System.Windows.Forms.MessageBoxButtons.OK,
            error ? System.Windows.Forms.MessageBoxIcon.Error : System.Windows.Forms.MessageBoxIcon.Information);
    }
}
