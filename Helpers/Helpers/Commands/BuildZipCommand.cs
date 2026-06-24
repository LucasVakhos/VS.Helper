// Commands\BuildZipCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using VS.Helper.Core.Zip;

namespace VS.Helper.Commands;

[Command(PackageIds.BuildZipCommand)]
internal sealed class BuildZipCommand : BaseCommand<BuildZipCommand>
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
            ZipBuildResult result = new ZipBuildEngine().Build(solutionPath);
            CopyZipToClipboard(result.ZipPath);

            ShowInfo(
                "ZIP создан по новой схеме и скопирован в буфер обмена:\n" +
                Path.GetFileName(result.ZipPath) + "\n\n" +
                "Файлов в архиве: " + result.IncludedFiles.Count + "\n" +
                "Конфиг: " + result.ConfigPath);
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
        }
    }

    private static bool TryGetSolutionPath(out string solutionPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        solutionPath = null;

        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is not DTE dte)
            return false;

        solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static void CopyZipToClipboard(string zipPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            throw new FileNotFoundException("ZIP-файл не найден.", zipPath);

        StringCollection files = new StringCollection { zipPath };
        System.Windows.Forms.DataObject data = new System.Windows.Forms.DataObject();
        data.SetFileDropList(files);
        System.Windows.Forms.Clipboard.SetDataObject(data, true);
    }

    private static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "VS.Helper", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "Ошибка сборки ZIP", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
    }
}
