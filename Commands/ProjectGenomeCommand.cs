using EnvDTE;
using System;
using System.IO;
using System.Text;
using System.Threading;
using VS.Helper.Core.OS;

namespace VS.Helper.Commands;

[Community.VisualStudio.Toolkit.Command(PackageIds.ProjectGenomeCommand)]
internal sealed class ProjectGenomeCommand : BaseCommand<ProjectGenomeCommand>
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
            string capturedSolutionPath = solutionPath;
            EngineBus bus = new EngineBus().Register(new ProjectGenomeCommandCore());
            EngineContext context = new EngineContext(capturedSolutionPath);

            EngineCommandResult result = await Task.Run(
                () => bus.ExecuteAsync(ProjectGenomeCommandCore.CommandName, context, CancellationToken.None),
                CancellationToken.None);

            if (!result.Success)
            {
                ShowError(result.Message);
                return;
            }

            string reportPath = result.Data.TryGetValue("OutputPath", out string outputPath) ? outputPath : context.WorkDirectory;
            StringBuilder message = new();
            message.AppendLine("Project Genome создан.");
            message.AppendLine();
            message.AppendLine("Проектов: " + Value(result, "Projects"));
            message.AppendLine("C# файлов: " + Value(result, "Files"));
            message.AppendLine("Строк кода: " + Value(result, "Lines"));
            message.AppendLine("TODO/FIXME/HACK: " + Value(result, "Todos"));
            message.AppendLine();
            message.AppendLine("Отчёт:");
            message.AppendLine(reportPath);
            message.AppendLine();
            message.AppendLine("Это первый кирпич VS.Helper OS: память проекта без подвеса IDE.");

            ShowInfo(message.ToString());
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
        }
    }

    private static string Value(EngineCommandResult result, string key)
    {
        return result.Data.TryGetValue(key, out string value) ? value : "0";
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is not DTE dte)
            return false;

        string solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "VS.Helper / Project Genome", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        System.Windows.Forms.MessageBox.Show(message, "Ошибка Project Genome", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
    }
}
