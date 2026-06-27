using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace VS.Helper.Core.Zip;

internal sealed class ZipBuildConfig
{
    public const string LegacyFileName = "VS.Helper.Zip.xml";
    public const string FileNameTemplate = "{Solution.Name}.config";

    public static string GetFileName(string solutionPath)
    {
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        return $"{solutionName}.config";
    }

    public static string GetConfigPath(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        return Path.Combine(solutionDir, GetFileName(solutionPath));
    }

    public static string GetLegacyConfigPath(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        return Path.Combine(solutionDir, LegacyFileName);
    }

    public static bool TryMigrateLegacyConfig(string solutionPath, out string configPath, out bool migrated)
    {
        configPath = GetConfigPath(solutionPath);
        migrated = false;

        if (File.Exists(configPath))
            return true;

        string legacyPath = GetLegacyConfigPath(solutionPath);
        if (!File.Exists(legacyPath))
            return false;

        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? Environment.CurrentDirectory);

        try
        {
            File.Move(legacyPath, configPath);
        }
        catch
        {
            File.Copy(legacyPath, configPath, true);
        }

        migrated = true;
        return true;
    }

    public string Root { get; set; } = ".";
    public string OutputDir { get; set; } = "_zip";
    public string ArchiveName { get; set; } = "{Solution.Name}.zip";
    public string StartProject { get; set; } = string.Empty;
    public bool IncludeProjectClosure { get; set; } = true;
    public bool IncludeManifest { get; set; } = true;
    public bool IncludeSolutionFiles { get; set; } = true;
    public List<string> Include { get; } = new();
    public List<string> Exclude { get; } = new();

    public static ZipBuildConfig LoadOrCreateDefault(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        TryMigrateLegacyConfig(solutionPath, out string configPath, out _);

        if (!File.Exists(configPath))
            return CreateDefault(solutionPath);

        XDocument document = XDocument.Load(configPath);
        XElement root = document.Root ?? throw new InvalidOperationException($"Файл {Path.GetFileName(configPath)} пустой.");

        ZipBuildConfig config = new()
        {
            Root = Read(root, "Root", "."),
            OutputDir = Read(root, "OutputDir", "_zip"),
            ArchiveName = NormalizeArchiveName(Read(root, "ArchiveName", "{Solution.Name}.zip")),
            StartProject = Read(root, "StartProject", string.Empty),
            IncludeProjectClosure = ReadBool(root, "IncludeProjectClosure", true),
            IncludeManifest = ReadBool(root, "IncludeManifest", true),
            IncludeSolutionFiles = ReadBool(root, "IncludeSolutionFiles", true),
        };

        // ВАЖНО: старые конфиги VS.Helper уже писали <Path>, а новая схема писала <File>/<Pattern>.
        // Поддерживаем оба формата, иначе Build Zip выглядел как старая упаковка папки.
        config.Include.AddRange(ReadItems(root, "Include", "File", "Path"));
        config.Exclude.AddRange(ReadItems(root, "Exclude", "Pattern", "Path"));

        if (config.Include.Count == 0 && !config.IncludeProjectClosure)
            config.IncludeProjectClosure = true;

        EnsureDefaultExcludes(config);
        return config;
    }

    public static ZipBuildConfig CreateDefault(string solutionPath)
    {
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);

        ZipBuildConfig config = new()
        {
            Root = ".",
            OutputDir = "_zip",
            ArchiveName = "{Solution.Name}.zip",
            IncludeProjectClosure = true,
            IncludeManifest = true,
            IncludeSolutionFiles = true,
        };

        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;

        string firstProject = SolutionProjectScanner.GetProjects(solutionPath).FirstOrDefault() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(firstProject))
        {
            config.StartProject = ZipPath.GetRelativePath(solutionDir, firstProject);
        }

        config.Include.Add(Path.GetFileName(solutionPath));
        config.Include.Add(GetFileName(solutionPath));
        config.Include.Add(Path.GetFileNameWithoutExtension(solutionPath) + ".csproj");
        config.Include.Add("source.extension.vsixmanifest");
        config.Include.Add("VSCommandTable.vsct");
        config.Include.Add("README.md");
        config.Include.Add("ABOUT.md");
        config.Include.Add("LICENSE*");
        config.Include.Add("Directory.Build.*");
        config.Include.Add("Directory.Packages.props");
        config.Include.Add("global.json");
        config.Include.Add("NuGet.config");
        config.Include.Add(".editorconfig");
        config.Include.Add(".gitattributes");
        config.Include.Add(".gitignore");
        foreach (string project in SolutionProjectScanner.GetProjects(solutionPath))
        {
            string projectDir = Path.GetDirectoryName(project);
            if (string.IsNullOrWhiteSpace(projectDir))
                continue;

            string relative = ZipPath.GetRelativePath(solutionDir, projectDir);
            if (!string.IsNullOrWhiteSpace(relative) && relative != "." &&
                !config.Include.Any(x => string.Equals(ZipPath.NormalizeRelative(x), ZipPath.NormalizeRelative(relative), StringComparison.OrdinalIgnoreCase)))
                config.Include.Add(relative);
        }

        EnsureDefaultExcludes(config);
        return config;
    }

    public void Save(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        string path = GetConfigPath(solutionPath);

        XDocument document = new(
            new XElement("VSHelperZip",
                new XElement("Root", Root),
                new XElement("OutputDir", OutputDir),
                new XElement("ArchiveName", ArchiveName),
                new XElement("StartProject", StartProject),
                new XElement("IncludeProjectClosure", IncludeProjectClosure),
                new XElement("IncludeSolutionFiles", IncludeSolutionFiles),
                new XElement("IncludeManifest", IncludeManifest),
                new XElement("Include", Include.Select(x => new XElement("File", x))),
                new XElement("Exclude", Exclude.Select(x => new XElement("Pattern", x)))));

        document.Save(path);
    }

    private static string NormalizeArchiveName(string archiveName)
    {
        if (string.IsNullOrWhiteSpace(archiveName))
            return "{Solution.Name}.zip";

        string value = archiveName.Trim();

        // Старые конфиги часто создавали датированные ZIP или жёстко VS.Helper.zip.
        // Новый контракт Build Zip: архив всегда {Solution.Name}.zip.
        if (value.Contains("$(Date)", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("$(Time)", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("VS.Helper.zip", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("$(SolutionName).zip", StringComparison.OrdinalIgnoreCase))
            return "{Solution.Name}.zip";

        return value;
    }

    private static string Read(XElement root, string name, string defaultValue)
        => (string?)root.Element(name) ?? defaultValue;

    private static bool ReadBool(XElement root, string name, bool defaultValue)
    {
        string value = Read(root, name, defaultValue.ToString());
        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }

    private static IEnumerable<string> ReadItems(XElement root, string parentName, params string[] itemNames)
    {
        XElement parent = root.Element(parentName);
        if (parent == null)
            return Enumerable.Empty<string>();

        return parent.Elements()
            .Where(x => itemNames.Any(name => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase)))
            .Select(x => ((string?)x)?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!);
    }

    private static void EnsureDefaultExcludes(ZipBuildConfig config)
    {
        string[] defaults =
        {
            "bin/**", "**/bin/**", "obj/**", "**/obj/**", ".vs/**", "**/.vs/**",
            ".git/**", "**/.git/**", ".idea/**", "**/.idea/**", ".vscode/**", "**/.vscode/**",
            "packages/**", "**/packages/**", "TestResults/**", "**/TestResults/**",
            "_zip/**", "**/_zip/**", "VSIX/**", "**/VSIX/**",
            "*.user", "*.suo", "*.cache", "*.log", "*.zip", "*.nupkg", "*.vsix", "*.pdb",
            "VS.Helper.Zip.xml", "**/*.bak", "**/*.tmp"
        };

        foreach (string item in defaults)
            if (!config.Exclude.Any(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase)))
                config.Exclude.Add(item);
    }
}
