using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildConfigStore
{
    public const string ConfigFileName = "VS.Helper.Zip.xml";

    private readonly ZipProjectGraphReader _projectGraphReader = new ZipProjectGraphReader();

    public string GetConfigPath(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        return Path.Combine(solutionDir, ConfigFileName);
    }

    public ZipBuildConfig LoadOrCreateInMemory(string solutionPath)
    {
        string configPath = GetConfigPath(solutionPath);
        return File.Exists(configPath) ? Load(configPath) : CreateDefault(solutionPath);
    }

    public string WriteDefaultConfig(string solutionPath, bool overwrite)
    {
        string configPath = GetConfigPath(solutionPath);
        if (File.Exists(configPath) && !overwrite)
            return configPath;

        ZipBuildConfig config = CreateDefault(solutionPath);
        Save(configPath, config);
        return configPath;
    }

    public ZipBuildConfig CreateDefault(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        ZipBuildConfig config = new ZipBuildConfig
        {
            Root = "$(SolutionDir)",
            OutputDir = "$(SolutionDir)",
            ArchiveName = "$(SolutionName)_$(Date)_$(Time).zip",
            StartProject = string.Empty,
            IncludeManifest = true,
            IncludeProjectClosure = true
        };

        config.Include.Add(Path.GetFileName(solutionPath));

        foreach (string projectPath in _projectGraphReader.GetProjectsFromSolution(solutionPath))
        {
            string projectDir = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrWhiteSpace(projectDir))
                config.Include.Add(ZipPathTools.GetRelativePath(solutionDir, projectDir));
        }

        foreach (string optional in new[]
        {
            "Directory.Build.props",
            "Directory.Build.targets",
            "Directory.Packages.props",
            "NuGet.config",
            ZipBuildConfigStore.ConfigFileName,
            "README.md",
            "LICENSE.txt",
            ".gitignore"
        })
        {
            if (File.Exists(Path.Combine(solutionDir, optional)))
                config.Include.Add(optional);
        }

        foreach (string exclude in ZipBuildConfig.DefaultExcludes())
            config.Exclude.Add(exclude);

        return config;
    }

    private static ZipBuildConfig Load(string configPath)
    {
        XDocument document = XDocument.Load(configPath);
        XElement root = document.Root;

        if (root == null)
            throw new InvalidOperationException("Пустой конфиг ZIP: " + configPath);

        ZipBuildConfig config = new ZipBuildConfig
        {
            Root = ReadValue(root, "Root") ?? "$(SolutionDir)",
            OutputDir = ReadValue(root, "OutputDir") ?? "$(SolutionDir)",
            ArchiveName = ReadValue(root, "ArchiveName") ?? "$(SolutionName)_$(Date)_$(Time).zip",
            StartProject = ReadValue(root, "StartProject"),
            IncludeManifest = ReadBool(root, "IncludeManifest", true),
            IncludeProjectClosure = ReadBool(root, "IncludeProjectClosure", true)
        };

        foreach (XElement element in root.Element("Include")?.Elements("Path") ?? Enumerable.Empty<XElement>())
        {
            string value = ((string)element)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                config.Include.Add(value);
        }

        foreach (XElement element in root.Element("Exclude")?.Elements("Path") ?? Enumerable.Empty<XElement>())
        {
            string value = ((string)element)?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                config.Exclude.Add(value);
        }

        if (config.Include.Count == 0 && !config.IncludeProjectClosure)
            throw new InvalidOperationException(ConfigFileName + ": секция <Include> пуста и IncludeProjectClosure=false.");

        return config;
    }

    private static void Save(string configPath, ZipBuildConfig config)
    {
        XDocument document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("VSHelperZip",
                new XElement("Root", config.Root),
                new XElement("OutputDir", config.OutputDir),
                new XElement("ArchiveName", config.ArchiveName),
                new XElement("StartProject", config.StartProject ?? string.Empty),
                new XElement("IncludeManifest", config.IncludeManifest),
                new XElement("IncludeProjectClosure", config.IncludeProjectClosure),
                new XElement("Include", config.Include.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Select(x => new XElement("Path", x))),
                new XElement("Exclude", config.Exclude.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Select(x => new XElement("Path", x)))));

        using (StreamWriter writer = new StreamWriter(configPath, false, new UTF8Encoding(true)))
            document.Save(writer);
    }

    private static string ReadValue(XElement root, string name)
    {
        string value = (string)root.Element(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool ReadBool(XElement root, string name, bool defaultValue)
    {
        string value = ReadValue(root, name);
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        bool parsed;
        return bool.TryParse(value, out parsed) ? parsed : defaultValue;
    }
}
