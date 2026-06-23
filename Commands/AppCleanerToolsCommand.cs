// Commands\VSHelperToolsCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using System.Windows.Forms;

using WinFormsMessageBox = System.Windows.Forms.MessageBox;

namespace VS.Helper.Commands;

[Command(PackageIds.VSHelperToolsCommand)]
internal sealed class VSHelperToolsCommand : BaseCommand<VSHelperToolsCommand>
{
    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Command.Enabled = VSHelperToolsHelper.TryGetOpenedSolution(out _);
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (!VSHelperToolsHelper.TryGetOpenedSolution(out VSHelperSolutionInfo solution))
        {
            VSHelperToolsHelper.ShowInfo("Нет открытого .sln/.slnx.");
            return;
        }

        using VSHelperToolsDialog dialog = new VSHelperToolsDialog(solution);

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        try
        {
            VSHelperOptions options = dialog.GetOptions();
            options.SolutionPath = solution.SolutionPath;
            options.SolutionDir = solution.SolutionDir;

            if (options.Item == VSHelperComboTodoItems.DeleteNonProjectFiles)
            {
                System.Windows.Forms.DialogResult confirm = System.Windows.Forms.MessageBox.Show(
                    "Операция может удалить файлы, которые не входят в выбранный .csproj.\n\n" +
                    "Перед удалением будут созданы .bak-копии, если включён Backup.\n\n" +
                    "Продолжить?",
                    "VS.Helper / VSHelper",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (confirm != System.Windows.Forms.DialogResult.Yes)
                    return;
            }

            string log = VSHelperEngine.Run(options);
            VSHelperLogDialog.ShowLog(log);
        }
        catch (Exception ex)
        {
            VSHelperToolsHelper.ShowError(ex.ToString());
        }
    }
}
