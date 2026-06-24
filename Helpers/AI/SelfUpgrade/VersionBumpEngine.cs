// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using SysProcess = System.Diagnostics.Process;
namespace VS.Helper.AI;

internal static class VersionBumpEngine
{
    private const string DefaultVersion = "1.0.2.3520";

    public static string Bump(string solutionDir)
    {
        string current = FindCurrentVersion(solutionDir) ?? DefaultVersion;
        string next = IncrementBuild(current);

        foreach (string file in Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories))
            ReplaceVersions(file, next);

        foreach (string file in Directory.GetFiles(solutionDir, "AssemblyInfo.cs", SearchOption.AllDirectories))
            ReplaceVersions(file, next);

        foreach (string file in Directory.GetFiles(solutionDir, "source.extension.vsixmanifest", SearchOption.AllDirectories))
            ReplaceVsixVersion(file, next);

        foreach (string file in Directory.GetFiles(solutionDir, "source.extension.cs", SearchOption.AllDirectories))
            ReplaceSourceExtensionVersion(file, next);

        return next;
    }

    private static string? FindCurrentVersion(string solutionDir)
    {
        foreach (string file in Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            var match = Regex.Match(text, @"<(Version|AssemblyVersion|FileVersion|InformationalVersion)>(?<v>\d+\.\d+\.\d+\.\d+)</\1>");
            if (match.Success)
                return match.Groups["v"].Value;
        }

        return null;
    }

    private static string IncrementBuild(string version)
    {
        string[] parts = version.Split('.');
        if (parts.Length != 4 || !int.TryParse(parts[3], out int build))
            return DefaultVersion;

        parts[3] = (build + 1).ToString();
        return string.Join(".", parts);
    }

    private static void ReplaceVersions(string file, string version)
    {
        string text = File.ReadAllText(file);
        string updated = Regex.Replace(text,
            @"(?<open><(Version|AssemblyVersion|FileVersion|InformationalVersion)>)(?<v>\d+\.\d+\.\d+\.\d+)(?<close></(Version|AssemblyVersion|FileVersion|InformationalVersion)>)",
            m => m.Groups["open"].Value + version + m.Groups["close"].Value);

        updated = Regex.Replace(updated,
            @"Assembly(File)?Version\(""\d+\.\d+\.\d+\.\d+""\)",
            m => m.Value.StartsWith("AssemblyFileVersion", StringComparison.Ordinal)
                ? "AssemblyFileVersion(\"" + version + "\")"
                : "AssemblyVersion(\"" + version + "\")");

        if (!string.Equals(text, updated, StringComparison.Ordinal))
            File.WriteAllText(file, updated);
    }

    private static void ReplaceVsixVersion(string file, string version)
    {
        string text = File.ReadAllText(file);
        string updated = Regex.Replace(text, "Version=\"\\d+\\.\\d+\\.\\d+(\\.\\d+)?\"", "Version=\"" + version + "\"");
        if (!string.Equals(text, updated, StringComparison.Ordinal))
            File.WriteAllText(file, updated);
    }

    private static void ReplaceSourceExtensionVersion(string file, string version)
    {
        string text = File.ReadAllText(file);
        string updated = Regex.Replace(text, "public const string Version = \"[^\"]+\";", "public const string Version = \"" + version + "\";");
        if (!string.Equals(text, updated, StringComparison.Ordinal))
            File.WriteAllText(file, updated);
    }
}
