// Commands\CommitStampSyncGitCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VS.Helper.Commands;

[Command(PackageIds.CommitStampSyncGitCommand)]
internal sealed class CommitStampSyncGitCommand : BaseCommand<CommitStampSyncGitCommand>
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

        string solutionPath = GetSolutionPath();
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
        {
            ShowInfo("Нет открытого Solution.");
            return;
        }

        string solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDir))
        {
            ShowInfo("Не удалось определить папку Solution.");
            return;
        }

        string stamp = Path.GetFileNameWithoutExtension(solutionPath) + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            string repoRoot = await GetGitRepositoryRootAsync(solutionDir);
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                ShowInfo("Git-репозиторий не найден для текущего Solution.");
                return;
            }

            StringBuilder log = new StringBuilder();

            GitResult addResult = await RunGitAsync(repoRoot, "add -A");
            log.AppendLine("> git add -A");
            AppendResult(log, addResult);

            GitResult diffResult = await RunGitAsync(repoRoot, "diff --cached --quiet", true);
            if (diffResult.ExitCode == 0)
            {
                ShowInfo("Нет изменений для commit.");
                return;
            }

            GitResult commitResult = await RunGitAsync(repoRoot, "commit -m \"" + EscapeGitArgument(stamp) + "\"");
            log.AppendLine("> git commit -m \"" + stamp + "\"");
            AppendResult(log, commitResult);

            if (commitResult.ExitCode != 0)
            {
                ShowError("Commit не выполнен.\n\n" + log.ToString());
                return;
            }

            GitResult pushResult = await RunGitAsync(repoRoot, "push");
            log.AppendLine("> git push");
            AppendResult(log, pushResult);

            if (pushResult.ExitCode != 0)
            {
                ShowError("Commit создан, но git push не выполнен.\n\n" + log.ToString());
                return;
            }

            ShowInfo("Commit + Sync Git выполнено.\n\n" + stamp);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string solutionPath = GetSolutionPath();
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static string GetSolutionPath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        DTE dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
        if (dte == null)
            return null;

        if (dte.Solution == null)
            return null;

        return dte.Solution.FullName;
    }

    private static async Task<string> GetGitRepositoryRootAsync(string startDirectory)
    {
        GitResult result = await RunGitAsync(startDirectory, "rev-parse --show-toplevel", true);
        if (result.ExitCode != 0)
            return null;

        string root = result.Output.Trim();
        return Directory.Exists(root) ? root : null;
    }

    private static async Task<GitResult> RunGitAsync(string workingDirectory, string arguments)
    {
        return await RunGitAsync(workingDirectory, arguments, false);
    }

    private static async Task<GitResult> RunGitAsync(string workingDirectory, string arguments, bool allowNonZeroExitCode)
    {
        return await Task.Run(delegate
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "git";
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                GitResult result = new GitResult(process.ExitCode, output, error);

                if (!allowNonZeroExitCode && result.ExitCode != 0)
                    return result;

                return result;
            }
        });
    }

    private static void AppendResult(StringBuilder log, GitResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output))
            log.AppendLine(result.Output.Trim());

        if (!string.IsNullOrWhiteSpace(result.Error))
            log.AppendLine(result.Error.Trim());

        if (result.ExitCode != 0)
            log.AppendLine("ExitCode: " + result.ExitCode);

        log.AppendLine();
    }

    private static string EscapeGitArgument(string value)
    {
        if (value == null)
            return string.Empty;

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static void ShowInfo(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        System.Windows.Forms.MessageBox.Show(
            message,
            "VS.Helper",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        System.Windows.Forms.MessageBox.Show(
            message,
            "Commit + Sync Git",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }

    private sealed class GitResult
    {
        public GitResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public int ExitCode { get; private set; }
        public string Output { get; private set; }
        public string Error { get; private set; }
    }
}
