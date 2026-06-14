// Commands\BuildZipCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio;

namespace VS.Helper.Commands;

[Command(PackageIds.BuildZipCommand)]
internal sealed class BuildZipCommand : BaseCommand<BuildZipCommand>
{
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

        string? solutionPath = dte.Solution?.FullName;

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

        VsShellUtilities.ShowMessageBox(
            ServiceProvider.GlobalProvider,
            message,
            "VS.Helper",
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    private static void ShowError(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        VsShellUtilities.ShowMessageBox(
            ServiceProvider.GlobalProvider,
            message,
            "Ошибка сборки ZIP",
            OLEMSGICON.OLEMSGICON_CRITICAL,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is not DTE dte)
            return false;

        string? solutionPath = dte.Solution?.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static string BuildZipArchive(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath)
            ?? throw new InvalidOperationException("Не удалось определить папку Solution.");

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

                foreach (string reference in projectReferences)
                    if (!visitedProjects.Contains(reference))
                        projects.Enqueue(reference);
            }

            foreach (string file in files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (IsIgnoredPath(file))
                    continue;

                string relativePath = GetRelativePath(solutionDir, file);

                if (relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    relativePath.Equals("..", StringComparison.Ordinal))
                {
                    relativePath = Path.Combine("_External", Path.GetFileName(file));
                }

                string destination = Path.Combine(tempRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
            }

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempRoot, zipPath, CompressionLevel.Optimal, false);
            return zipPath;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
            catch
            {
                // Временная папка не должна ломать результат/сообщение пользователю.
            }
        }
    }

    private static List<string> GetProjectsFromSolution(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath)!;
        string extension = Path.GetExtension(solutionPath);

        return extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            ? GetProjectsFromSlnx(solutionPath, solutionDir)
            : GetProjectsFromSln(solutionPath, solutionDir);
    }

    private static List<string> GetProjectsFromSlnx(string solutionPath, string solutionDir)
    {
        XDocument document = XDocument.Load(solutionPath);

        return document
            .Descendants("Project")
            .Select(x => (string?)x.Attribute("Path"))
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFullPath(Path.Combine(solutionDir, x!)))
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

    private static List<string> GetFilesFromProject(string projectPath, out List<string> projectReferences)
    {
        string projectDir = Path.GetDirectoryName(projectPath)!;
        XDocument document = XDocument.Load(projectPath);

        List<string> result = new();
        projectReferences = new List<string>();

        foreach (XElement element in document.Descendants().Where(x => x.Name.LocalName == "ProjectReference"))
        {
            string? include = (string?)element.Attribute("Include");
            if (string.IsNullOrWhiteSpace(include))
                continue;

            string referencePath = Path.GetFullPath(Path.Combine(projectDir, include));
            if (File.Exists(referencePath) && !IsIgnoredPath(referencePath))
                projectReferences.Add(referencePath);
        }

        foreach (XElement element in document.Descendants().Where(x => ProjectItemNames.Contains(x.Name.LocalName)))
        {
            string? include = (string?)element.Attribute("Include");
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


    private static string GetRelativePath(string baseDirectory, string filePath)
    {
        Uri baseUri = new(AppendDirectorySeparatorChar(Path.GetFullPath(baseDirectory)));
        Uri fileUri = new(Path.GetFullPath(filePath));
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

    private static bool IsIgnoredPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string[] parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            part.Equals(".vs", StringComparison.OrdinalIgnoreCase));
    }
}
