// Commands\BuildSolutionCommand.cs
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.Commands;

[Command(PackageIds.BuildSolutionCommand)]
internal sealed class BuildSolutionCommand : BaseCommand<BuildSolutionCommand>
{
    private const string DefaultBaseVersion = "1.0.2.3520";

    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Command.Enabled = HasOpenSolution();
        Command.Visible = true;
    }

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is not DTE2 dte)
        {
            ShowError("Не удалось получить DTE.");
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
            List<string> projects = GetProjectsFromSolution(solutionPath);
            VersionUpdateResult versionResult = UpdateProjectVersions(projects);

            SolutionBuild solutionBuild = dte.Solution.SolutionBuild;
            solutionBuild.Build(true);

            int errors = solutionBuild.LastBuildInfo;

            if (errors == 0)
            {
                ShowInfo(
                    "Build Solution выполнен.\n\n" +
                    "Версия обновлена: " + versionResult.VersionText + "\n" +
                    "Проектов обновлено: " + versionResult.UpdatedProjects);
            }
            else
            {
                ShowError(
                    "Build Solution завершился с ошибками: " + errors + "\n\n" +
                    "Версия обновлена: " + versionResult.VersionText + "\n" +
                    "Проектов обновлено: " + versionResult.UpdatedProjects);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private static bool HasOpenSolution()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) is not DTE dte)
            return false;

        string solutionPath = dte.Solution == null ? null : dte.Solution.FullName;
        return !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
    }

    private static VersionUpdateResult UpdateProjectVersions(IEnumerable<string> projectPaths)
    {
        List<string> projects = projectPaths
            .Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string currentVersion = FindCurrentVersion(projects) ?? DefaultBaseVersion;
        string newVersion = IncrementBuildVersion(currentVersion);

        int updated = 0;

        foreach (string projectPath in projects)
        {
            bool changed = false;

            if (UpdateCsprojVersion(projectPath, newVersion))
                changed = true;

            string projectDir = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrWhiteSpace(projectDir))
            {
                string assemblyInfoPath = Path.Combine(projectDir, "Properties", "AssemblyInfo.cs");
                if (File.Exists(assemblyInfoPath) && UpdateAssemblyInfoVersion(assemblyInfoPath, newVersion))
                    changed = true;
            }

            if (changed)
                updated++;
        }

        return new VersionUpdateResult(newVersion, updated);
    }

    private static string FindCurrentVersion(IEnumerable<string> projectPaths)
    {
        foreach (string projectPath in projectPaths)
        {
            string version = FindVersionInCsproj(projectPath);
            if (!string.IsNullOrWhiteSpace(version))
                return version;

            string projectDir = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrWhiteSpace(projectDir))
                continue;

            string assemblyInfoPath = Path.Combine(projectDir, "Properties", "AssemblyInfo.cs");
            version = FindVersionInAssemblyInfo(assemblyInfoPath);
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        return null;
    }

    private static string FindVersionInCsproj(string projectPath)
    {
        try
        {
            XDocument document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);

            foreach (string propertyName in new[] { "Version", "AssemblyVersion", "FileVersion" })
            {
                XElement element = document
                    .Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == propertyName && IsFourPartVersion(x.Value));

                if (element != null)
                    return NormalizeToTargetVersion(element.Value.Trim());
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string FindVersionInAssemblyInfo(string assemblyInfoPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyInfoPath) || !File.Exists(assemblyInfoPath))
            return null;

        string text = File.ReadAllText(assemblyInfoPath);
        Match match = Regex.Match(
            text,
            "Assembly(File)?Version\\(\\\"(?<version>\\d+\\.\\d+\\.\\d+\\.\\d+)\\\"\\)",
            RegexOptions.IgnoreCase);

        return match.Success ? NormalizeToTargetVersion(match.Groups["version"].Value) : null;
    }

    private static bool UpdateCsprojVersion(string projectPath, string version)
    {
        XDocument document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
        XNamespace ns = document.Root == null ? XNamespace.None : document.Root.Name.Namespace;

        XElement propertyGroup = document.Root?
            .Elements(ns + "PropertyGroup")
            .FirstOrDefault(x => x.Attribute("Condition") == null)
            ?? document.Root?.Elements(ns + "PropertyGroup").FirstOrDefault();

        if (propertyGroup == null)
            return false;

        bool changed = false;

        changed |= SetOrAddProperty(propertyGroup, ns, "Version", version);
        changed |= SetOrAddProperty(propertyGroup, ns, "AssemblyVersion", version);
        changed |= SetOrAddProperty(propertyGroup, ns, "FileVersion", version);
        changed |= SetOrAddProperty(propertyGroup, ns, "InformationalVersion", version);

        if (changed)
            document.Save(projectPath);

        return changed;
    }

    private static bool SetOrAddProperty(XElement propertyGroup, XNamespace ns, string name, string value)
    {
        XElement element = propertyGroup.Elements(ns + name).FirstOrDefault();

        if (element == null)
        {
            propertyGroup.Add(new XElement(ns + name, value));
            return true;
        }

        if (string.Equals(element.Value.Trim(), value, StringComparison.Ordinal))
            return false;

        element.Value = value;
        return true;
    }

    private static bool UpdateAssemblyInfoVersion(string assemblyInfoPath, string version)
    {
        string oldText = File.ReadAllText(assemblyInfoPath);
        string newText = Regex.Replace(
            oldText,
            "AssemblyVersion\\(\\\"\\d+\\.\\d+\\.\\d+\\.\\d+\\\"\\)",
            "AssemblyVersion(\"" + version + "\")");

        newText = Regex.Replace(
            newText,
            "AssemblyFileVersion\\(\\\"\\d+\\.\\d+(\\.\\d+)?(\\.\\d+)?\\\"\\)",
            "AssemblyFileVersion(\"" + version + "\")");

        if (string.Equals(oldText, newText, StringComparison.Ordinal))
            return false;

        File.WriteAllText(assemblyInfoPath, newText);
        return true;
    }

    private static string IncrementBuildVersion(string version)
    {
        Match match = Regex.Match(version ?? string.Empty, "^(?<major>\\d+)\\.(?<minor>\\d+)\\.(?<patch>\\d+)\\.(?<build>\\d+)$");
        if (!match.Success)
            version = DefaultBaseVersion;

        System.Version parsed = new System.Version(version);
        return parsed.Major + "." + parsed.Minor + "." + parsed.Build + "." + (parsed.Revision + 1);
    }

    private static string NormalizeToTargetVersion(string version)
    {
        if (!IsFourPartVersion(version))
            return null;

        System.Version parsed = new System.Version(version);
        int build = Math.Max(parsed.Revision, 3520);

        return "1.0.2." + build;
    }

    private static bool IsFourPartVersion(string value)
    {
        return Regex.IsMatch(value?.Trim() ?? string.Empty, "^\\d+\\.\\d+\\.\\d+\\.\\d+$");
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
            "Build Solution",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }

    private sealed class VersionUpdateResult
    {
        public VersionUpdateResult(string versionText, int updatedProjects)
        {
            VersionText = versionText;
            UpdatedProjects = updatedProjects;
        }

        public string VersionText { get; }
        public int UpdatedProjects { get; }
    }
}
