using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VS.Helper.Core.Zip;

internal sealed class ZipProjectGraphReader
{
    private static readonly HashSet<string> ProjectItemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

    public IReadOnlyList<string> GetProjectsFromSolution(string solutionPath)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        string extension = Path.GetExtension(solutionPath);

        return extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            ? GetProjectsFromSlnx(solutionPath, solutionDir)
            : GetProjectsFromSln(solutionPath, solutionDir);
    }

    public IReadOnlyList<string> GetProjectClosure(string solutionPath, string startProject)
    {
        string solutionDir = Path.GetDirectoryName(solutionPath);
        string start = string.IsNullOrWhiteSpace(startProject)
            ? null
            : ZipPathTools.ResolvePath(solutionDir, startProject);

        Queue<string> queue = new Queue<string>();

        if (!string.IsNullOrWhiteSpace(start) && File.Exists(start))
            queue.Enqueue(start);
        else
            foreach (string project in GetProjectsFromSolution(solutionPath))
                queue.Enqueue(project);

        HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (queue.Count > 0)
        {
            string projectPath = Path.GetFullPath(queue.Dequeue());

            if (!File.Exists(projectPath) || !visited.Add(projectPath))
                continue;

            foreach (string reference in GetProjectReferences(projectPath))
            {
                if (!visited.Contains(reference))
                    queue.Enqueue(reference);
            }
        }

        return visited.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<string> GetFilesFromProject(string projectPath)
    {
        string projectDir = Path.GetDirectoryName(projectPath);
        XDocument document = XDocument.Load(projectPath);
        List<string> result = new List<string> { Path.GetFullPath(projectPath) };

        foreach (XElement element in document.Descendants().Where(x => ProjectItemNames.Contains(x.Name.LocalName)))
        {
            string include = (string)element.Attribute("Include");
            if (string.IsNullOrWhiteSpace(include))
                continue;

            foreach (string file in ExpandProjectItem(projectDir, include))
            {
                if (File.Exists(file) && !ZipPathMatcher.IsIgnoredPath(file))
                    result.Add(file);
            }
        }

        // SDK-style projects include files implicitly. Add project folder contents, filtered by config/excludes later.
        if (!string.IsNullOrWhiteSpace(projectDir) && Directory.Exists(projectDir))
        {
            foreach (string file in Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories))
            {
                if (!ZipPathMatcher.IsIgnoredPath(file))
                    result.Add(Path.GetFullPath(file));
            }
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private IReadOnlyList<string> GetProjectReferences(string projectPath)
    {
        string projectDir = Path.GetDirectoryName(projectPath);
        XDocument document = XDocument.Load(projectPath);
        List<string> result = new List<string>();

        foreach (XElement element in document.Descendants().Where(x => x.Name.LocalName == "ProjectReference"))
        {
            string include = (string)element.Attribute("Include");
            if (string.IsNullOrWhiteSpace(include))
                continue;

            string referencePath = Path.GetFullPath(Path.Combine(projectDir, include));
            if (File.Exists(referencePath) && !ZipPathMatcher.IsIgnoredPath(referencePath))
                result.Add(referencePath);
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private IReadOnlyList<string> GetProjectsFromSlnx(string solutionPath, string solutionDir)
    {
        XDocument document = XDocument.Load(solutionPath);

        return document.Descendants()
            .Where(x => x.Name.LocalName == "Project")
            .Select(x => (string)x.Attribute("Path"))
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFullPath(Path.Combine(solutionDir, x)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> GetProjectsFromSln(string solutionPath, string solutionDir)
    {
        Regex regex = new Regex("Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"", RegexOptions.IgnoreCase);

        return File.ReadAllLines(solutionPath)
            .Select(line => regex.Match(line))
            .Where(match => match.Success)
            .Select(match => Path.GetFullPath(Path.Combine(solutionDir, match.Groups[1].Value)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IEnumerable<string> ExpandProjectItem(string projectDir, string include)
    {
        include = include.Replace('/', Path.DirectorySeparatorChar);

        if (!ZipPathTools.HasWildcard(include))
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
            searchRoot = string.IsNullOrWhiteSpace(rootPart) ? projectDir : Path.Combine(projectDir, rootPart);
            pattern = Path.GetFileName(normalized);
            searchOption = SearchOption.AllDirectories;
        }
        else
        {
            int firstWildcard = normalized.IndexOfAny(new[] { '*', '?' });
            int separatorBeforeWildcard = normalized.LastIndexOf(Path.DirectorySeparatorChar, Math.Max(0, firstWildcard));

            searchRoot = separatorBeforeWildcard >= 0 ? Path.Combine(projectDir, normalized.Substring(0, separatorBeforeWildcard)) : projectDir;
            pattern = separatorBeforeWildcard >= 0 ? normalized.Substring(separatorBeforeWildcard + 1) : normalized;
            searchOption = SearchOption.TopDirectoryOnly;
        }

        if (!Directory.Exists(searchRoot))
            yield break;

        foreach (string file in Directory.EnumerateFiles(searchRoot, pattern, searchOption))
        {
            if (!ZipPathMatcher.IsIgnoredPath(file))
                yield return Path.GetFullPath(file);
        }
    }
}
