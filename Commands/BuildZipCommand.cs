// Commands\BuildZipCommand.cs
using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.Commands;

[Command(PackageIds.BuildZipCommand)]
internal sealed class BuildZipCommand : BaseCommand<BuildZipCommand>
{
    private const string ConfigFileName = "VS.Helper.Zip.xml";

    private static readonly HashSet<string> ProjectItemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Compile",
        "EmbeddedResource",
        "Content",
        "None",
        "Resource",
        "Page",
        "ApplicationDefinition",
        "AdditionalFiles",
        "Analyzer",
        "NativeReference"
    };

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
            string zipPath = BuildZipArchive(solutionPath);
            ShowInfo($"ZIP собран:\n{zipPath}");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
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
            "Ошибка сборки ZIP",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is not DTE dte)
            return false;

        string solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static string BuildZipArchive(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDir))
            throw new InvalidOperationException("Не удалось определить папку Solution.");

        ZipBuildConfig config = ZipBuildConfig.Load(solutionPath);

        return config == null
            ? BuildZipArchiveBySolution(solutionPath)
            : BuildZipArchiveByConfig(solutionPath, config);
    }

    private static string BuildZipArchiveByConfig(string solutionPath, ZipBuildConfig config)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        string rootDir = ResolvePath(solutionDir, ReplaceVariables(config.Root, solutionDir, solutionName));
        if (!Directory.Exists(rootDir))
            throw new InvalidOperationException($"Корневая папка из конфига не найдена:\n{rootDir}");

        string archiveName = string.IsNullOrWhiteSpace(config.ArchiveName)
            ? solutionName + ".zip"
            : ReplaceVariables(config.ArchiveName, solutionDir, solutionName);

        if (!archiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            archiveName += ".zip";

        string outputDir = string.IsNullOrWhiteSpace(config.OutputDir)
            ? solutionDir
            : ResolvePath(solutionDir, ReplaceVariables(config.OutputDir, solutionDir, solutionName));

        Directory.CreateDirectory(outputDir);

        string zipPath = Path.Combine(outputDir, archiveName);
        string tempRoot = Path.Combine(Path.GetTempPath(), "VS.Helper.Zip", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);

        try
        {
            HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);

            foreach (string include in config.Include)
            {
                foreach (string file in ExpandConfiguredPath(rootDir, include))
                    files.Add(file);
            }

            if (!string.IsNullOrWhiteSpace(config.StartProject))
            {
                string startProjectPath = ResolvePath(rootDir, config.StartProject);

                if (File.Exists(startProjectPath))
                {
                    files.Add(startProjectPath);

                    Queue<string> projects = new();
                    projects.Enqueue(startProjectPath);

                    HashSet<string> visitedProjects = new(StringComparer.OrdinalIgnoreCase);

                    while (projects.Count > 0)
                    {
                        string projectPath = Path.GetFullPath(projects.Dequeue());

                        if (!File.Exists(projectPath) || !visitedProjects.Add(projectPath))
                            continue;

                        files.Add(projectPath);

                        List<string> projectReferences;

                        foreach (string file in GetFilesFromProject(projectPath, out projectReferences))
                            files.Add(file);

                        foreach (string reference in projectReferences)
                            if (!visitedProjects.Contains(reference))
                                projects.Enqueue(reference);
                    }
                }
            }

            foreach (string file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(file) || IsSamePath(file, zipPath))
                    continue;

                string relativePath = GetRelativePath(rootDir, file);

                if (relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    relativePath.Equals("..", StringComparison.Ordinal))
                {
                    relativePath = Path.Combine("_External", Path.GetFileName(file));
                }

                if (IsIgnoredPath(file) || IsExcluded(relativePath, config.Exclude))
                    continue;

                string destination = Path.Combine(tempRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, true);
            }

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempRoot, zipPath, CompressionLevel.Optimal, false);
            return zipPath;
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static string BuildZipArchiveBySolution(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDir))
            throw new InvalidOperationException("Не удалось определить папку Solution.");

        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        string zipPath = Path.Combine(solutionDir, solutionName + ".zip");
        string tempRoot = Path.Combine(Path.GetTempPath(), "VS.Helper.Zip", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);

        try
        {
            HashSet<string> files = new(StringComparer.OrdinalIgnoreCase)
            {
                Path.GetFullPath(solutionPath)
            };

            Queue<string> projects = new(GetProjectsFromSolution(solutionPath));
            HashSet<string> visitedProjects = new(StringComparer.OrdinalIgnoreCase);

            while (projects.Count > 0)
            {
                string projectPath = Path.GetFullPath(projects.Dequeue());

                if (!File.Exists(projectPath) || !visitedProjects.Add(projectPath))
                    continue;

                files.Add(projectPath);

                List<string> projectReferences;

                foreach (string file in GetFilesFromProject(projectPath, out projectReferences))
                    files.Add(file);

                foreach (string file in GetFilesFromProjectDirectory(projectPath))
                    files.Add(file);

                foreach (string reference in projectReferences)
                    if (!visitedProjects.Contains(reference))
                        projects.Enqueue(reference);
            }

            foreach (string file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (IsIgnoredPath(file) || IsSamePath(file, zipPath))
                    continue;

                string relativePath = GetRelativePath(solutionDir, file);

                if (relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    relativePath.Equals("..", StringComparison.Ordinal))
                {
                    relativePath = Path.Combine("_External", Path.GetFileName(file));
                }

                string destination = Path.Combine(tempRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, true);
            }

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempRoot, zipPath, CompressionLevel.Optimal, false);
            return zipPath;
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static List<string> GetProjectsFromSolution(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        string extension = Path.GetExtension(solutionPath);

        return extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            ? GetProjectsFromSlnx(solutionPath, solutionDir)
            : GetProjectsFromSln(solutionPath, solutionDir);
    }

    private static List<string> GetProjectsFromSlnx(string solutionPath, string solutionDir)
    {
        XDocument document = XDocument.Load(solutionPath);

        return document
            .Descendants().Where(x => x.Name.LocalName == "Project")
            .Select(x => (string)x.Attribute("Path"))
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFullPath(Path.Combine(solutionDir, x)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetProjectsFromSln(string solutionPath, string solutionDir)
    {
        Regex regex = new("Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"", RegexOptions.IgnoreCase);

        return File.ReadAllLines(solutionPath)
            .Select(line => regex.Match(line))
            .Where(match => match.Success)
            .Select(match => Path.GetFullPath(Path.Combine(solutionDir, match.Groups[1].Value)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> GetFilesFromProjectDirectory(string projectPath)
    {
        string projectDir = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
            yield break;

        foreach (string file in Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories))
        {
            if (!IsIgnoredPath(file))
                yield return Path.GetFullPath(file);
        }
    }

    private static List<string> GetFilesFromProject(string projectPath, out List<string> projectReferences)
    {
        string projectDir = Path.GetDirectoryName(projectPath);
        XDocument document = XDocument.Load(projectPath);

        List<string> result = new();
        projectReferences = new List<string>();

        foreach (XElement element in document.Descendants().Where(x => x.Name.LocalName == "ProjectReference"))
        {
            string include = (string)element.Attribute("Include");
            if (string.IsNullOrWhiteSpace(include))
                continue;

            string referencePath = Path.GetFullPath(Path.Combine(projectDir, include));
            if (File.Exists(referencePath) && !IsIgnoredPath(referencePath))
                projectReferences.Add(referencePath);
        }

        foreach (XElement element in document.Descendants().Where(x => ProjectItemNames.Contains(x.Name.LocalName)))
        {
            string include = (string)element.Attribute("Include");
            if (string.IsNullOrWhiteSpace(include))
                continue;

            foreach (string file in ExpandProjectItem(projectDir, include))
            {
                if (File.Exists(file) && !IsIgnoredPath(file))
                    result.Add(file);
            }
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> ExpandProjectItem(string projectDir, string include)
    {
        include = include.Replace('/', Path.DirectorySeparatorChar);

        if (!HasWildcard(include))
        {
            yield return Path.GetFullPath(Path.Combine(projectDir, include));
            yield break;
        }

        string normalized = include.Replace('/', Path.DirectorySeparatorChar);
        string searchRoot;
        string pattern;
        SearchOption searchOption;

        int doubleStarIndex = normalized.IndexOf("**", StringComparison.Ordinal);
        if (doubleStarIndex >= 0)
        {
            string rootPart = normalized.Substring(0, doubleStarIndex).TrimEnd(Path.DirectorySeparatorChar);
            searchRoot = string.IsNullOrWhiteSpace(rootPart)
                ? projectDir
                : Path.Combine(projectDir, rootPart);
            pattern = Path.GetFileName(normalized);
            searchOption = SearchOption.AllDirectories;
        }
        else
        {
            int firstWildcard = normalized.IndexOfAny(new[] { '*', '?' });
            int separatorBeforeWildcard = normalized.LastIndexOf(Path.DirectorySeparatorChar, Math.Max(0, firstWildcard));

            searchRoot = separatorBeforeWildcard >= 0
                ? Path.Combine(projectDir, normalized.Substring(0, separatorBeforeWildcard))
                : projectDir;
            pattern = separatorBeforeWildcard >= 0
                ? normalized.Substring(separatorBeforeWildcard + 1)
                : normalized;
            searchOption = SearchOption.TopDirectoryOnly;
        }

        if (!Directory.Exists(searchRoot))
            yield break;

        foreach (string file in Directory.EnumerateFiles(searchRoot, pattern, searchOption))
        {
            if (!IsIgnoredPath(file))
                yield return Path.GetFullPath(file);
        }
    }

    private static IEnumerable<string> ExpandConfiguredPath(string rootDir, string include)
    {
        if (string.IsNullOrWhiteSpace(include))
            yield break;

        include = include.Replace('/', Path.DirectorySeparatorChar);

        if (HasWildcard(include))
        {
            foreach (string file in ExpandProjectItem(rootDir, include))
                yield return file;

            yield break;
        }

        string fullPath = ResolvePath(rootDir, include);

        if (File.Exists(fullPath))
        {
            yield return fullPath;
            yield break;
        }

        if (!Directory.Exists(fullPath))
            yield break;

        foreach (string file in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories))
            yield return Path.GetFullPath(file);
    }

    private static bool IsExcluded(string relativePath, IEnumerable<string> excludes)
    {
        string normalized = NormalizeRelativePath(relativePath);

        foreach (string exclude in excludes ?? Enumerable.Empty<string>())
        {
            string pattern = NormalizeRelativePath(exclude);

            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            if (WildcardMatch(normalized, pattern))
                return true;

            string segmentPattern = pattern.Trim('/');

            if (!segmentPattern.Contains("*") && normalized.Split('/').Any(x => x.Equals(segmentPattern, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private static bool WildcardMatch(string text, string pattern)
    {
        pattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    private static string NormalizeRelativePath(string path)
    {
        return (path ?? string.Empty)
            .Replace('\\', '/')
            .TrimStart('/')
            .TrimEnd('/');
    }

    private static string ResolvePath(string baseDir, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Path.GetFullPath(baseDir);

        path = Environment.ExpandEnvironmentVariables(path);

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(baseDir, path));
    }

    private static string ReplaceVariables(string value, string solutionDir, string solutionName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return value
            .Replace("$(SolutionDir)", AppendDirectorySeparatorChar(solutionDir))
            .Replace("$(SolutionName)", solutionName);
    }

    private static string GetRelativePath(string baseDirectory, string filePath)
    {
        Uri baseUri = new Uri(AppendDirectorySeparatorChar(Path.GetFullPath(baseDirectory)));
        Uri fileUri = new Uri(Path.GetFullPath(filePath));
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static bool HasWildcard(string path) => path.Contains('*') || path.Contains('?');

    private static bool IsSamePath(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string[] parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string fileName = Path.GetFileName(fullPath);

        if (fileName.EndsWith(".user", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".suo", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".cache", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            part.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
            part.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("packages", StringComparison.OrdinalIgnoreCase));
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        catch
        {
            // Временная папка не должна ломать результат/сообщение пользователю.
        }
    }

    private sealed class ZipBuildConfig
    {
        public string Root { get; private set; }
        public string OutputDir { get; private set; }
        public string ArchiveName { get; private set; }
        public string StartProject { get; private set; }
        public List<string> Include { get; } = new();
        public List<string> Exclude { get; } = new();

        public static ZipBuildConfig Load(string solutionPath)
        {
            string solutionDir = Path.GetDirectoryName(solutionPath);
            string configPath = Path.Combine(solutionDir, ConfigFileName);

            if (!File.Exists(configPath))
                return null;

            XDocument document = XDocument.Load(configPath);
            XElement root = document.Root;

            if (root == null)
                throw new InvalidOperationException($"Пустой конфиг: {configPath}");

            ZipBuildConfig config = new()
            {
                Root = ReadValue(root, "Root") ?? "$(SolutionDir)",
                OutputDir = ReadValue(root, "OutputDir") ?? "$(SolutionDir)",
                ArchiveName = ReadValue(root, "ArchiveName") ?? "$(SolutionName).zip",
                StartProject = ReadValue(root, "StartProject")
            };

            foreach (XElement element in root.Element("Include")?.Elements("Path") ?? Enumerable.Empty<XElement>())
            {
                string value = ((string)element).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    config.Include.Add(value);
            }

            foreach (XElement element in root.Element("Exclude")?.Elements("Path") ?? Enumerable.Empty<XElement>())
            {
                string value = ((string)element).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    config.Exclude.Add(value);
            }

            if (config.Include.Count == 0)
                throw new InvalidOperationException($"{ConfigFileName}: секция <Include> пуста.");

            return config;
        }

        private static string ReadValue(XElement root, string name)
        {
            string value = (string)root.Element(name);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
