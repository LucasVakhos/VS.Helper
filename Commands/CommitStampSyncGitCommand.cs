// Commands\CommitStampSyncGitCommand.cs
using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsStartInfo = System.Diagnostics.ProcessStartInfo;

namespace VS.Helper.Commands;

[Command(PackageIds.CommitStampSyncGitCommand)]
internal sealed class CommitStampSyncGitCommand : BaseCommand<CommitStampSyncGitCommand>
{
    private const string ConfigFileName = "VS.Helper.Zip.xml";
    private const string GitHubTokenUrl = "https://github.com/settings/tokens/new";

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

        string stamp = Path.GetFileNameWithoutExtension(solutionPath) + " " +
                       DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            string repoRoot = await GetGitRepositoryRootAsync(solutionDir);
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                ShowInfo("Git-репозиторий не найден для текущего Solution.");
                return;
            }

            GitCredentials creds = LoadGitCredentials(repoRoot);

            if (string.IsNullOrWhiteSpace(creds.UserName) || string.IsNullOrWhiteSpace(creds.Token))
            {
                OpenUrl(GitHubTokenUrl);

                ShowInfo(
                    "Создан или найден файл " + ConfigFileName + ".\n\n" +
                    "Заполни Git секцию внутри <VSHelperZip>:\n\n" +
                    "<VSHelperZip>\n" +
                    "  <Git>\n" +
                    "    <UserName>твой_логин</UserName>\n" +
                    "    <Token>твой_token</Token>\n" +
                    "    <TokenProtected></TokenProtected>\n" +
                    "  </Git>\n" +
                    "</VSHelperZip>\n\n" +
                    "Открыл страницу создания GitHub token.\n" +
                    "После заполнения XML запусти команду ещё раз.\n\n" +
                    "Token будет автоматически зашифрован через DPAPI Windows.");

                return;
            }

            string originUrl = await GetOriginUrlAsync(repoRoot);
            if (string.IsNullOrWhiteSpace(originUrl))
            {
                ShowError("Не удалось получить origin URL.");
                return;
            }

            string authRemoteUrl = BuildAuthRemoteUrl(originUrl, creds);

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

            GitResult commitResult = await RunGitAsync(
                repoRoot,
                "commit -m \"" + EscapeGitArgument(stamp) + "\"");

            log.AppendLine("> git commit -m \"" + stamp + "\"");
            AppendResult(log, commitResult);

            if (commitResult.ExitCode != 0)
            {
                ShowError("Commit не выполнен.\n\n" + log);
                return;
            }

            GitResult pullResult = await RunGitAsync(
                repoRoot,
                "pull --rebase " + QuoteGitArgument(authRemoteUrl));

            log.AppendLine("> git pull --rebase origin");
            AppendResultMasked(log, pullResult, authRemoteUrl);

            if (pullResult.ExitCode != 0)
            {
                string conflicts = await GetConflictReportAsync(repoRoot);

                ShowError(
                    "Commit создан, но git pull --rebase не выполнен.\n\n" +
                    conflicts +
                    "\n\nДля отмены rebase можно выполнить:\n" +
                    "git rebase --abort\n\n" +
                    log);
                return;
            }

            GitResult pushResult = await RunGitAsync(
                repoRoot,
                "push " + QuoteGitArgument(authRemoteUrl));

            log.AppendLine("> git push origin");
            AppendResultMasked(log, pushResult, authRemoteUrl);

            if (pushResult.ExitCode != 0)
            {
                ShowError("Commit создан, pull выполнен, но git push не выполнен.\n\n" + log);
                return;
            }

            ShowInfo("Commit + Sync Git выполнено.\n\n" + stamp);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private static GitCredentials LoadGitCredentials(string repoRoot)
    {
        string cfg = Path.Combine(repoRoot, ConfigFileName);

        EnsureConfigFile(cfg);
        EnsureGitIgnore(repoRoot);

        XDocument doc = XDocument.Load(cfg);
        XElement root = EnsureElement(doc, "VSHelperZip");
        XElement git = EnsureElement(root, "Git");

        XElement userElement = EnsureElement(git, "UserName");
        XElement tokenElement = EnsureElement(git, "Token");
        XElement tokenProtectedElement = EnsureElement(git, "TokenProtected");

        string user = userElement.Value.Trim();
        string token = tokenElement.Value.Trim();
        string protectedToken = tokenProtectedElement.Value.Trim();

        if (!string.IsNullOrWhiteSpace(token))
        {
            tokenProtectedElement.Value = ProtectString(token);
            tokenElement.Value = string.Empty;
            doc.Save(cfg);

            return new GitCredentials(user, token);
        }

        if (!string.IsNullOrWhiteSpace(protectedToken))
        {
            token = UnprotectString(protectedToken);
        }

        doc.Save(cfg);
        return new GitCredentials(user, token);
    }

    private static void EnsureConfigFile(string cfg)
    {
        if (File.Exists(cfg))
            return;

        XDocument newDoc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("VSHelperZip",
                new XElement("Root", "$(SolutionDir)"),
                new XElement("OutputDir", "$(SolutionDir)"),
                new XElement("ArchiveName", "$(SolutionName).zip"),
                new XElement("StartProject", "RhymeContest.Blazor.Server\\RhymeContest.Blazor.Server.csproj"),

                new XElement("Git",
                    new XElement("UserName", "YOUR_GITHUB_LOGIN"),
                    new XElement("Token", ""),
                    new XElement("TokenProtected", "")
                ),

                new XElement("Include",
                    new XElement("Path", "RhymeContest.sln"),
                    new XElement("Path", "RhymeContest.Blazor.Server"),
                    new XElement("Path", "RhymeContest.Module"),
                    new XElement("Path", "RhymeContest.Module.Blazor"),
                    new XElement("Path", "Directory.Build.props"),
                    new XElement("Path", "Directory.Packages.props"),
                    new XElement("Path", "NuGet.config"),
                    new XElement("Path", "README.md")
                ),

                new XElement("Exclude",
                    new XElement("Path", "**/bin/**"),
                    new XElement("Path", "**/obj/**"),
                    new XElement("Path", "**/.vs/**"),
                    new XElement("Path", "**/.git/**"),
                    new XElement("Path", "**/node_modules/**"),
                    new XElement("Path", "**/packages/**"),
                    new XElement("Path", "**/*.user"),
                    new XElement("Path", "**/*.suo"),
                    new XElement("Path", "**/*.pdb"),
                    new XElement("Path", "**/*.cache"),
                    new XElement("Path", "**/*.log"),
                    new XElement("Path", "**/appsettings.Development.json"),
                    new XElement("Path", "**/appsettings.Production.json"),
                    new XElement("Path", "**/*.db"),
                    new XElement("Path", "**/*.sqlite")
                )
            )
        );

        newDoc.Save(cfg);
    }

    private static void EnsureGitIgnore(string repoRoot)
    {
        string gitIgnore = Path.Combine(repoRoot, ".gitignore");

        if (!File.Exists(gitIgnore))
        {
            File.WriteAllText(gitIgnore, ConfigFileName + Environment.NewLine, Encoding.UTF8);
            return;
        }

        string text = File.ReadAllText(gitIgnore, Encoding.UTF8);

        if (text.IndexOf(ConfigFileName, StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        if (!text.EndsWith(Environment.NewLine))
            text += Environment.NewLine;

        text += ConfigFileName + Environment.NewLine;
        File.WriteAllText(gitIgnore, text, Encoding.UTF8);
    }

    private static XElement EnsureElement(XDocument doc, string name)
    {
        if (doc.Root == null)
            doc.Add(new XElement(name));

        if (doc.Root.Name.LocalName != name)
            throw new InvalidOperationException(
                "Некорректный корневой элемент в " + ConfigFileName +
                ". Ожидается <" + name + ">.");

        return doc.Root;
    }

    private static XElement EnsureElement(XElement parent, string name)
    {
        XElement element = parent.Element(name);
        if (element == null)
        {
            element = new XElement(name, string.Empty);
            parent.Add(element);
        }

        return element;
    }

    private static string ProtectString(string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(value ?? string.Empty);
        byte[] protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedData);
    }

    private static string UnprotectString(string value)
    {
        byte[] protectedData = Convert.FromBase64String(value);
        byte[] data = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(data);
    }

    private static async Task<string> GetOriginUrlAsync(string repoRoot)
    {
        GitResult result = await RunGitAsync(repoRoot, "remote get-url origin", true);

        if (result.ExitCode != 0)
            return null;

        return result.Output.Trim();
    }

    private static string BuildAuthRemoteUrl(string originUrl, GitCredentials creds)
    {
        if (originUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            string cleanUrl = originUrl;

            if (cleanUrl.IndexOf("@github.com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int schemeIndex = cleanUrl.IndexOf("https://", StringComparison.OrdinalIgnoreCase);
                int atIndex = cleanUrl.IndexOf("@github.com", StringComparison.OrdinalIgnoreCase);

                if (schemeIndex >= 0 && atIndex > schemeIndex)
                    cleanUrl = "https://" + cleanUrl.Substring(atIndex + 1);
            }

            string user = Uri.EscapeDataString(creds.UserName);
            string token = Uri.EscapeDataString(creds.Token);

            return cleanUrl.Replace("https://", "https://" + user + ":" + token + "@");
        }

        throw new InvalidOperationException(
            "Сейчас поддерживается только HTTPS origin.\n\n" +
            "Текущий origin:\n" + originUrl + "\n\n" +
            "Поставь HTTPS:\n" +
            "git remote set-url origin https://github.com/USER/REPO.git");
    }

    private static async Task<string> GetConflictReportAsync(string repoRoot)
    {
        GitResult status = await RunGitAsync(repoRoot, "status --porcelain", true);
        List<string> conflicts = new List<string>();

        using (StringReader reader = new StringReader(status.Output ?? string.Empty))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.Length < 3)
                    continue;

                string code = line.Substring(0, 2);
                string file = line.Substring(3);

                if (code == "UU" || code == "AA" || code == "DD" || code == "AU" ||
                    code == "UA" || code == "DU" || code == "UD")
                {
                    conflicts.Add(file);
                }
            }
        }

        if (conflicts.Count == 0)
            return "Конфликтующие файлы не найдены. Подробности смотри в логе git.";

        StringBuilder report = new StringBuilder();
        report.AppendLine("Найдены конфликты:");

        foreach (string file in conflicts)
            report.AppendLine("- " + file);

        report.AppendLine();
        report.AppendLine("Открой эти файлы, исправь маркеры <<<<<<< ======= >>>>>>>, затем выполни:");
        report.AppendLine("git add -A");
        report.AppendLine("git rebase --continue");

        return report.ToString();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            DiagnosticsStartInfo psi = new DiagnosticsStartInfo();
            psi.FileName = url;
            psi.UseShellExecute = true;

            DiagnosticsProcess.Start(psi);
        }
        catch
        {
            // Не мешаем основной команде, если браузер не открылся.
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
            DiagnosticsStartInfo startInfo = new DiagnosticsStartInfo();
            startInfo.FileName = "git";
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            using (DiagnosticsProcess process = new DiagnosticsProcess())
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

    private static void AppendResultMasked(StringBuilder log, GitResult result, string secret)
    {
        GitResult masked = new GitResult(
            result.ExitCode,
            MaskSecret(result.Output, secret),
            MaskSecret(result.Error, secret));

        AppendResult(log, masked);
    }

    private static string MaskSecret(string value, string secret)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(secret))
            return value;

        return value.Replace(secret, "https://***:***@github.com/***/***.git");
    }

    private static string EscapeGitArgument(string value)
    {
        if (value == null)
            return string.Empty;

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string QuoteGitArgument(string value)
    {
        if (value == null)
            return "\"\"";

        return "\"" + value.Replace("\"", "\\\"") + "\"";
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

    private sealed class GitCredentials
    {
        public GitCredentials(string userName, string token)
        {
            UserName = userName ?? string.Empty;
            Token = token ?? string.Empty;
        }

        public string UserName { get; private set; }
        public string Token { get; private set; }
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