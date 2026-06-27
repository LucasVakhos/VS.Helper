using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.Core.Versioning;

internal sealed class VsixVersionUpdateResult
{
    public string ManifestPath { get; set; } = string.Empty;
    public Version OldVersion { get; set; } = new(0, 0, 0, 0);
    public Version NewVersion { get; set; } = new(0, 0, 0, 0);
    public bool Updated { get; set; }
    public int UpdatedFiles { get; set; }

    public string Message => Updated
        ? $"VSIX version: {OldVersion} → {NewVersion}; files: {UpdatedFiles}"
        : "VSIX version: manifest не найден или версия не изменена";
}

internal static class VsixVersionService
{
    private static readonly string[] SkipFolders =
    {
        "bin", "obj", ".vs", ".git", "_zip", "packages", "VSIX"
    };

    public static Version Increment(Version version)
    {
        int major = Math.Max(0, version.Major);
        int minor = version.Minor < 0 ? 0 : version.Minor;
        int build = version.Build < 0 ? 0 : version.Build;
        int revision = version.Revision < 0 ? 0 : version.Revision;
        return new Version(major, minor, build, revision + 1);
    }

    public static VsixVersionUpdateResult IncrementManifestVersion(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        string manifestPath = Path.Combine(solutionDir, "source.extension.vsixmanifest");

        VsixVersionUpdateResult result = new() { ManifestPath = manifestPath };
        if (!File.Exists(manifestPath))
            return result;

        XDocument document = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
        XElement? identity = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Identity");
        XAttribute? versionAttribute = identity?.Attribute("Version");
        if (versionAttribute == null || !Version.TryParse(versionAttribute.Value, out Version? oldVersion))
            return result;

        Version newVersion = Increment(oldVersion);
        int updated = 0;

        foreach (string file in EnumerateVersionFiles(solutionDir))
        {
            if (TryUpdateFile(file, newVersion))
                updated++;
        }

        result.OldVersion = oldVersion;
        result.NewVersion = newVersion;
        result.Updated = updated > 0;
        result.UpdatedFiles = updated;
        return result;
    }

    private static IEnumerable<string> EnumerateVersionFiles(string solutionDir)
    {
        return Directory.EnumerateFiles(solutionDir, "*.*", SearchOption.AllDirectories)
            .Where(IsVersionFile)
            .Where(x => !IsInSkippedFolder(solutionDir, x));
    }

    private static bool IsVersionFile(string file)
    {
        string name = Path.GetFileName(file);
        string ext = Path.GetExtension(file);

        return name.Equals("source.extension.vsixmanifest", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("source.extension", StringComparison.OrdinalIgnoreCase) && ext.Equals(".cs", StringComparison.OrdinalIgnoreCase)
            || name.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInSkippedFolder(string root, string file)
    {
        string relative = Path.GetRelativePath(root, file);
        string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => SkipFolders.Any(skip => part.Equals(skip, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool TryUpdateFile(string file, Version version)
    {
        try
        {
            string text = File.ReadAllText(file);
            string updated = text;
            string value = version.ToString();

            if (Path.GetFileName(file).Equals("source.extension.vsixmanifest", StringComparison.OrdinalIgnoreCase))
            {
                updated = Regex.Replace(
                    updated,
                    "(?<prefix><Identity\\b[^>]*\\sVersion=\")(?<version>[^\"]+)(?<suffix>\")",
                    "${prefix}" + value + "${suffix}",
                    RegexOptions.IgnoreCase);
            }
            else if (Path.GetFileName(file).StartsWith("source.extension", StringComparison.OrdinalIgnoreCase)
                && Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                updated = Regex.Replace(
                    updated,
                    "(?<prefix>public\\s+const\\s+string\\s+Version\\s*=\\s*\")(?<version>[^\"]+)(?<suffix>\"\\s*;)",
                    "${prefix}" + value + "${suffix}");
            }
            else if (Path.GetExtension(file).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                updated = UpdateProjectXmlText(updated, value);
            }
            else if (Path.GetFileName(file).Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
            {
                updated = Regex.Replace(updated, "AssemblyVersion\\(\"[^\"]+\"\\)", "AssemblyVersion(\"" + value + "\")");
                updated = Regex.Replace(updated, "AssemblyFileVersion\\(\"[^\"]+\"\\)", "AssemblyFileVersion(\"" + value + "\")");
            }

            if (updated == text)
                return false;

            File.WriteAllText(file, updated);
            return true;
        }
        catch
        {
            // Инкремент версии не должен ломать Build Zip.
            return false;
        }
    }

    private static string UpdateProjectXmlText(string text, string version)
    {
        string updated = text;
        string[] names = { "Version", "AssemblyVersion", "FileVersion", "InformationalVersion" };

        foreach (string name in names)
        {
            string pattern = "(?<open><" + name + ">)(?<value>[^<]*)(?<close></" + name + ">)";
            if (Regex.IsMatch(updated, pattern, RegexOptions.IgnoreCase))
            {
                updated = Regex.Replace(updated, pattern, "${open}" + version + "${close}", RegexOptions.IgnoreCase);
            }
            else if (name == "Version")
            {
                updated = Regex.Replace(
                    updated,
                    "(?<pg><PropertyGroup>\\s*)",
                    "${pg}<Version>" + version + "</Version>\r\n    ",
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromSeconds(1));
            }
        }

        updated = Regex.Replace(updated, "<CreateVsixContainer>False</CreateVsixContainer>", "<CreateVsixContainer>True</CreateVsixContainer>", RegexOptions.IgnoreCase);
        return updated;
    }
}
