// Commands\BuildZipCommand.cs
using EnvDTE;
using System;
using System.Collections.Specialized;
using System.IO;
using VS.Helper.Core.Handoff;
using VS.Helper.Core.Zip;

namespace VS.Helper.Commands;

[Command(PackageIds.BuildZipCommand)]
internal sealed class BuildZipCommand : BaseCommand<BuildZipCommand>
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
            ShowInfo("Не удалось получить DTE.");
            return;
        }

        string solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
        {
            ShowInfo("Нет открытого Solution.");
            return;
        }

        try
        {
            ZipBuildResult result = new ZipBuildService().Build(solutionPath);
            CopyZipToClipboard(result.ZipPath);

            ShowInfo(
                "ZIP создан новым движком и скопирован в буфер обмена.\n\n" +
                "Файл: " + Path.GetFileName(result.ZipPath) + "\n" +
                "Файлов внутри: " + result.FileCount + "\n" +
                "Версия VSIX не изменялась: Build Zip только упаковывает проект. Версию поднимает Self Upgrade.\n" +
                "Конфиг: " + Path.GetFileName(result.ConfigPath) +
                (result.UsedGeneratedConfig ? "\n\nКонфиг был создан автоматически. Проверь VS.Helper.Zip.xml и запускай Build Zip ещё раз." : string.Empty) +
                "\n\nПосле OK откроется браузер без новой вкладки ChatGPT. Вставь ZIP сюда и напиши: продолжаем.");

            BrowserHandoffService.OpenBrowser();
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
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

    private static void CopyZipToClipboard(string zipPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            throw new FileNotFoundException("ZIP-файл не найден.", zipPath);

        StringCollection files = new() { zipPath };
        System.Windows.Forms.DataObject data = new();
        data.SetFileDropList(files);
        System.Windows.Forms.Clipboard.SetDataObject(data, true);
    }

    private static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "VS.Helper / Build Zip", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "Ошибка Build Zip", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
    }
}
