// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.AI;

internal static class VersionBumpEngine
{
    private const string DefaultVersion = "2026.2.1.1";
    private static readonly XNamespace VsixNs = "http://schemas.microsoft.com/developer/vsx-schema/2011";

    public static string Bump(string solutionDir)
    {
        string current = FindCurrentVersion(solutionDir) ?? DefaultVersion;
        string next = IncrementBuild(current);

        foreach (string file in Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories))
            ReplaceVersions(file, next);

        foreach (string file in Directory.GetFiles(solutionDir, "AssemblyInfo.cs", SearchOption.AllDirectories))
            ReplaceVersions(file, next);

        foreach (string file in Directory.GetFiles(solutionDir, "source.extension.vsixmanifest", SearchOption.AllDirectories))
        {
            try
            {
                ReplaceVsixIdentityVersion(file, next);
            }
            catch
            {
                // Игнорируем сломанные дубликаты в служебных папках, чтобы апгрейд не падал.
            }
        }

        foreach (string file in Directory.GetFiles(solutionDir, "source.extension.cs", SearchOption.AllDirectories))
            ReplaceSourceExtensionVersion(file, next);

        return next;
    }

    private static string? FindCurrentVersion(string solutionDir)
    {
        foreach (string file in Directory.GetFiles(solutionDir, "source.extension.vsixmanifest", SearchOption.AllDirectories).OrderBy(x => x.Length))
        {
            string? version = TryReadVsixIdentityVersion(file);
            if (!string.IsNullOrWhiteSpace(version))
                return NormalizeVersion(version);
        }

        foreach (string file in Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories).OrderBy(x => x.Length))
        {
            string text = File.ReadAllText(file);
            var match = Regex.Match(text, @"<(Version|AssemblyVersion|FileVersion|InformationalVersion)>(?<v>\d+\.\d+\.\d+(?:\.\d+)?)</\1>");
            if (match.Success)
                return NormalizeVersion(match.Groups["v"].Value);
        }

        return null;
    }

    private static string? TryReadVsixIdentityVersion(string file)
    {
        try
        {
            XDocument document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
            XElement? identity = document.Descendants(VsixNs + "Identity").FirstOrDefault()
                ?? document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Identity");
            return identity?.Attribute("Version")?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeVersion(string version)
    {
        string[] parts = version.Split('.');
        if (parts.Length == 4 && parts.All(p => int.TryParse(p, out _)))
            return version;
        if (parts.Length == 3 && parts.All(p => int.TryParse(p, out _)))
            return version + ".0";
        return DefaultVersion;
    }

    private static string IncrementBuild(string version)
    {
        string[] parts = NormalizeVersion(version).Split('.');
        if (parts.Length != 4 || !int.TryParse(parts[3], out int build))
            return DefaultVersion;

        parts[3] = (build + 1).ToString();
        return string.Join(".", parts);
    }

    private static void ReplaceVersions(string file, string version)
    {
        string text = File.ReadAllText(file);
        string updated = Regex.Replace(text,
            @"(?<open><(Version|AssemblyVersion|FileVersion|InformationalVersion)>)(?<v>\d+\.\d+\.\d+(?:\.\d+)?)(?<close></(Version|AssemblyVersion|FileVersion|InformationalVersion)>)",
            m => m.Groups["open"].Value + version + m.Groups["close"].Value);

        updated = Regex.Replace(updated,
            @"Assembly(File)?Version\(""\d+\.\d+\.\d+(?:\.\d+)?""\)",
            m => m.Value.StartsWith("AssemblyFileVersion", StringComparison.Ordinal)
                ? "AssemblyFileVersion(\"" + version + "\")"
                : "AssemblyVersion(\"" + version + "\")");

        if (!string.Equals(text, updated, StringComparison.Ordinal))
            File.WriteAllText(file, updated);
    }

    private static void ReplaceVsixIdentityVersion(string file, string version)
    {
        XDocument document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        XElement? identity = document.Descendants(VsixNs + "Identity").FirstOrDefault()
            ?? document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Identity");

        if (identity == null)
            return;

        string? oldVersion = identity.Attribute("Version")?.Value;
        if (string.Equals(oldVersion, version, StringComparison.Ordinal))
            return;

        identity.SetAttributeValue("Version", version);
        document.Save(file, SaveOptions.DisableFormatting);
    }

    private static void ReplaceSourceExtensionVersion(string file, string version)
    {
        string text = File.ReadAllText(file);
        string updated = Regex.Replace(text, "public const string Version = \"[^\"]+\";", "public const string Version = \"" + version + "\";");
        if (!string.Equals(text, updated, StringComparison.Ordinal))
            File.WriteAllText(file, updated);
    }
}
