// Commands\AppCleanerToolsCommand.cs
using EnvDTE;
using System.Windows.Forms;

using WinFormsMessageBox = System.Windows.Forms.MessageBox;

namespace VS.Helper.Commands;

[Command(PackageIds.AppCleanerToolsCommand)]
internal sealed class AppCleanerToolsCommand : BaseCommand<AppCleanerToolsCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Command.Enabled = AppCleanerToolsHelper.TryGetOpenedSolution(out _);
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (!AppCleanerToolsHelper.TryGetOpenedSolution(out AppCleanerSolutionInfo solution))
        {
            AppCleanerToolsHelper.ShowInfo("Нет открытого .sln/.slnx.");
            return;
        }

        using AppCleanerToolsDialog dialog = new AppCleanerToolsDialog(solution);

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        try
        {
            AppCleanerOptions options = dialog.GetOptions();
            options.SolutionPath = solution.SolutionPath;
            options.SolutionDir = solution.SolutionDir;

            if (options.Item == ComboTodoItems.DeleteNonProjectFiles)
            {
                System.Windows.Forms.DialogResult confirm = System.Windows.Forms.MessageBox.Show(
                    "Операция может удалить файлы, которые не входят в выбранный .csproj.\n\n" +
                    "Перед удалением будут созданы .bak-копии, если включён Backup.\n\n" +
                    "Продолжить?",
                    "VS.Helper / AppCleaner",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (confirm != System.Windows.Forms.DialogResult.Yes)
                    return;
            }

            string log = AppCleanerEngine.Run(options);
            AppCleanerLogDialog.ShowLog(log);
        }
        catch (Exception ex)
        {
            AppCleanerToolsHelper.ShowError(ex.ToString());
        }
    }
}
