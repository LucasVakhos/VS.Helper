using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.Core.Zip;

internal static class SolutionProjectScanner
{
    public static List<string> GetProjects(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        string extension = Path.GetExtension(solutionPath);

        return extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            ? GetProjectsFromSlnx(solutionPath, solutionDir)
            : GetProjectsFromSln(solutionPath, solutionDir);
    }

    private static List<string> GetProjectsFromSlnx(string solutionPath, string solutionDir)
    {
        XDocument document = XDocument.Load(solutionPath);
        return document.Descendants()
            .Where(x => x.Name.LocalName == "Project")
            .Select(x => (string?)x.Attribute("Path") ?? (string?)x.Attribute("path"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Path.GetFullPath(Path.Combine(solutionDir, x!)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetProjectsFromSln(string solutionPath, string solutionDir)
    {
        Regex regex = new("Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.(?:csproj|vbproj|fsproj|vcxproj))\"", RegexOptions.IgnoreCase);
        return File.ReadAllLines(solutionPath)
            .Select(line => regex.Match(line))
            .Where(match => match.Success)
            .Select(match => Path.GetFullPath(Path.Combine(solutionDir, match.Groups[1].Value)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
