// Commands\CreateZipConfigCommand.cs
using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DteProject = EnvDTE.Project;
using DteProjectItem = EnvDTE.ProjectItem;
using DteProjectItems = EnvDTE.ProjectItems;
using DteProjects = EnvDTE.Projects;

namespace VS.Helper.Commands;

[Command(PackageIds.CreateZipConfigCommand)]
internal sealed class CreateZipConfigCommand : BaseCommand<CreateZipConfigCommand>
{
    private const string ConfigFileName = "VS.Helper.Zip.xml";

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

        string solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrWhiteSpace(solutionDir))
        {
            ShowInfo("Не удалось определить папку Solution.");
            return;
        }

        string configPath = Path.Combine(solutionDir, ConfigFileName);

        try
        {
            if (!File.Exists(configPath))
            {
                string startupProjectPath = GetStartupProjectPath(dte, solutionDir);
                string startupProjectName = GetProjectName(startupProjectPath);
                string solutionFileName = Path.GetFileName(solutionPath);

                File.WriteAllText(configPath, CreateDefaultConfig(solutionFileName, startupProjectPath, startupProjectName));
            }

            dte.ItemOperations.OpenFile(configPath);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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

    private static string GetStartupProjectPath(DTE dte, string solutionDir)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            object startupProjectsObject = dte.Solution.SolutionBuild.StartupProjects;

            if (startupProjectsObject is Array startupProjects && startupProjects.Length > 0)
            {
                object value = startupProjects.GetValue(0);
                string startupProject = Convert.ToString(value);

                if (!string.IsNullOrWhiteSpace(startupProject))
                {
                    DteProject project = FindProjectByUniqueName(dte.Solution.Projects, startupProject);
                    if (project != null && !string.IsNullOrWhiteSpace(project.FullName) && File.Exists(project.FullName))
                        return MakeRelativePath(solutionDir, project.FullName);

                    if (startupProject.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                        return startupProject;
                }
            }
        }
        catch
        {
            // Если VS не отдала startup project, ниже возьмём первый .csproj из solution.
        }

        string firstProject = GetProjectFiles(dte.Solution.Projects)
            .FirstOrDefault(x => x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(firstProject)
            ? "MyProject\\MyProject.csproj"
            : MakeRelativePath(solutionDir, firstProject);
    }

    private static DteProject FindProjectByUniqueName(DteProjects projects, string uniqueName)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (DteProject project in EnumerateProjects(projects))
        {
            try
            {
                if (string.Equals(project.UniqueName, uniqueName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(project.Name, uniqueName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(project.FullName, uniqueName, StringComparison.OrdinalIgnoreCase))
                    return project;
            }
            catch
            {
                // Некоторые служебные элементы Solution могут не отдавать свойства.
            }
        }

        return null;
    }

    private static IEnumerable<string> GetProjectFiles(DteProjects projects)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (DteProject project in EnumerateProjects(projects))
        {
            string fullName = null;

            try
            {
                fullName = project.FullName;
            }
            catch
            {
                // Игнорируем служебные элементы Solution.
            }

            if (!string.IsNullOrWhiteSpace(fullName) && File.Exists(fullName))
                yield return fullName;
        }
    }

    private static IEnumerable<DteProject> EnumerateProjects(DteProjects projects)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (projects == null)
            yield break;

        foreach (DteProject project in projects)
        {
            if (project == null)
                continue;

            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.ProjectItems != null)
            {
                foreach (DteProject child in EnumerateProjectItems(project.ProjectItems))
                    yield return child;
            }
            else
            {
                yield return project;
            }
        }
    }

    private static IEnumerable<DteProject> EnumerateProjectItems(DteProjectItems projectItems)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (DteProjectItem item in projectItems)
        {
            DteProject child = null;

            try
            {
                child = item.SubProject;
            }
            catch
            {
                // Игнорируем элементы без подпроекта.
            }

            if (child == null)
                continue;

            if (child.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && child.ProjectItems != null)
            {
                foreach (DteProject nested in EnumerateProjectItems(child.ProjectItems))
                    yield return nested;
            }
            else
            {
                yield return child;
            }
        }
    }

    private static string CreateDefaultConfig(string solutionFileName, string startupProjectPath, string startupProjectName)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<VSHelperZip>
  <!-- Корень, относительно которого считаются Include/Exclude. Обычно это папка Solution. -->
  <Root>$(SolutionDir)</Root>

  <!-- Куда положить архив. Можно использовать $(SolutionDir), $(SolutionName). -->
  <OutputDir>$(SolutionDir)</OutputDir>
  <ArchiveName>$(SolutionName).zip</ArchiveName>

  <!-- Запускаемый проект. По нему VS.Helper добавит сам .csproj, файлы проекта и ProjectReference. -->
  <StartProject>{XmlEscape(startupProjectPath)}</StartProject>

  <Git>
    <UserName>YOUR_GITHUB_LOGIN</UserName>
    <Token></Token>
    <TokenProtected></TokenProtected>
  </Git>

  <Include>
    <Path>{XmlEscape(solutionFileName)}</Path>
    <Path>{XmlEscape(GetProjectFolder(startupProjectPath, startupProjectName))}</Path>
    <!-- Примеры:
    <Path>RhymeContest.Module</Path>
    <Path>RhymeContest.Blazor.Server</Path>
    <Path>README.md</Path>
    <Path>Directory.Build.props</Path>
    -->
  </Include>

  <Exclude>
    <Path>bin</Path>
    <Path>obj</Path>
    <Path>.vs</Path>
    <Path>.git</Path>
    <Path>packages</Path>
    <Path>node_modules</Path>
    <Path>*.user</Path>
    <Path>*.suo</Path>
    <Path>*.pdb</Path>
    <Path>*.cache</Path>
    <Path>*.log</Path>
    <Path>appsettings.Development.json</Path>
  </Exclude>
</VSHelperZip>
";
    }

    private static string GetProjectFolder(string startupProjectPath, string startupProjectName)
    {
        string folder = Path.GetDirectoryName(startupProjectPath);

        if (!string.IsNullOrWhiteSpace(folder))
            return folder;

        return string.IsNullOrWhiteSpace(startupProjectName) ? startupProjectPath : startupProjectName;
    }

    private static string GetProjectName(string projectPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(projectPath);
        return string.IsNullOrWhiteSpace(fileName) ? "MyProject" : fileName;
    }

    private static string MakeRelativePath(string baseDirectory, string filePath)
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

    private static string XmlEscape(string value)
    {
        return System.Security.SecurityElement.Escape(value ?? string.Empty);
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
            "Ошибка создания ZIP-конфига",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Error);
    }
}
