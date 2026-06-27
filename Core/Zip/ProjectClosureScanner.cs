using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace VS.Helper.Core.Zip;

internal sealed class ProjectClosureScanner
{
    private static readonly HashSet<string> FileItemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Compile", "EmbeddedResource", "Content", "None", "Resource", "Page", "ApplicationDefinition",
        "AdditionalFiles", "Analyzer", "NativeReference", "VSCTCompile", "EntityDeploy", "SplashScreen"
    };

    public IEnumerable<string> Collect(string solutionPath, ZipBuildConfig config, string rootDir)
    {
        Queue<string> queue = new();
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

        string start = ResolveStartProject(solutionPath, config, rootDir);
        if (!string.IsNullOrWhiteSpace(start) && File.Exists(start))
            queue.Enqueue(start);
        else
            foreach (string project in SolutionProjectScanner.GetProjects(solutionPath))
                queue.Enqueue(project);

        while (queue.Count > 0)
        {
            string projectPath = Path.GetFullPath(queue.Dequeue());
            if (!File.Exists(projectPath) || !visited.Add(projectPath))
                continue;

            yield return projectPath;

            foreach (string file in CollectProjectDeclaredFiles(projectPath))
                yield return file;

            foreach (string reference in GetProjectReferences(projectPath))
            {
                yield return reference;
                if (!visited.Contains(reference))
                    queue.Enqueue(reference);
            }
        }
    }

    private static string ResolveStartProject(string solutionPath, ZipBuildConfig config, string rootDir)
    {
        if (!string.IsNullOrWhiteSpace(config.StartProject))
        {
            string configured = Path.GetFullPath(Path.Combine(rootDir, config.StartProject));
            if (File.Exists(configured))
                return configured;
        }

        return SolutionProjectScanner.GetProjects(solutionPath).FirstOrDefault() ?? string.Empty;
    }

    private static IEnumerable<string> CollectProjectDeclaredFiles(string projectPath)
    {
        string projectDir = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        XDocument document;

        try { document = XDocument.Load(projectPath); }
        catch { yield break; }

        List<string> removePatterns = CollectRemovePatterns(document);

        // Explicit Include/Update items are authoritative and may re-add files after broad Remove rules.
        foreach (XElement item in document.Descendants().Where(x => FileItemNames.Contains(x.Name.LocalName)))
        {
            string include = ReadIncludeOrUpdateAttribute(item);
            if (string.IsNullOrWhiteSpace(include) || include.Contains("$"))
                continue;

            foreach (string file in Expand(projectDir, include))
                if (File.Exists(file))
                    yield return file;
        }

        foreach (string companion in CollectCompanionFiles(projectPath))
            yield return companion;

        // SDK-style projects may omit Compile Include. Respect MSBuild Remove rules so legacy duplicate
        // folders (Helpers/**, Resources/**, etc.) do not get pulled back into the ZIP by the fallback scan.
        if (IsSdkStyle(document))
        {
            string[] patterns = { "*.cs", "*.razor", "*.cshtml", "*.resx", "*.xaml", "*.json", "*.config", "*.props", "*.targets" };
            foreach (string pattern in patterns)
            {
                foreach (string file in Directory.EnumerateFiles(projectDir, pattern, SearchOption.AllDirectories))
                {
                    string relative = ZipPath.NormalizeRelative(ZipPath.GetRelativePath(projectDir, file));
                    if (IsRemoved(relative, removePatterns))
                        continue;

                    yield return Path.GetFullPath(file);
                }
            }
        }
    }

    private static string ReadIncludeOrUpdateAttribute(XElement item)
        => ((string?)item.Attribute("Include")
            ?? (string?)item.Attribute("Update")
            ?? string.Empty).Trim();

    private static List<string> CollectRemovePatterns(XDocument document)
    {
        return document.Descendants()
            .Where(x => FileItemNames.Contains(x.Name.LocalName))
            .Select(x => ((string?)x.Attribute("Remove") ?? string.Empty).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("$"))
            .Select(x => ZipPath.NormalizeRelative(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsRemoved(string relative, IEnumerable<string> removePatterns)
    {
        foreach (string pattern in removePatterns)
        {
            if (GlobLikeMatch(relative, pattern))
                return true;
        }

        return false;
    }

    private static bool GlobLikeMatch(string relative, string pattern)
    {
        relative = ZipPath.NormalizeRelative(relative);
        pattern = ZipPath.NormalizeRelative(pattern);

        if (string.Equals(relative, pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        if (pattern.EndsWith("/**", StringComparison.OrdinalIgnoreCase))
        {
            string prefix = pattern.Substring(0, pattern.Length - 3).TrimEnd('/');
            return relative.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith("/**/*", StringComparison.OrdinalIgnoreCase))
        {
            string prefix = pattern.Substring(0, pattern.Length - 5).TrimEnd('/');
            return relative.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        }

        string regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(
            relative,
            regex,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static IEnumerable<string> CollectCompanionFiles(string projectPath)
    {
        string projectDir = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        string solutionNameConfig = Path.GetFileNameWithoutExtension(projectPath) + ".config";
        string[] names =
        {
            "source.extension.vsixmanifest", "VSCommandTable.vsct", solutionNameConfig, "app.config", "App.config",
            "packages.config", "README.md", "ABOUT.md", "LICENSE.txt", "Directory.Build.props", "Directory.Packages.props", "NuGet.config"
        };

        foreach (string name in names)
        {
            string path = Path.Combine(projectDir, name);
            if (File.Exists(path))
                yield return Path.GetFullPath(path);
        }
    }

    private static bool IsSdkStyle(XDocument document)
        => document.Root?.Attribute("Sdk") != null;

    private static IEnumerable<string> GetProjectReferences(string projectPath)
    {
        string projectDir = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        XDocument document;

        try { document = XDocument.Load(projectPath); }
        catch { yield break; }

        foreach (XElement item in document.Descendants().Where(x => x.Name.LocalName == "ProjectReference"))
        {
            string include = (string?)item.Attribute("Include") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(include))
                continue;

            string referencePath = Path.GetFullPath(Path.Combine(projectDir, include));
            if (File.Exists(referencePath))
                yield return referencePath;
        }
    }

    private static IEnumerable<string> Expand(string projectDir, string include)
    {
        include = include.Replace('/', Path.DirectorySeparatorChar);
        if (!include.Contains("*") && !include.Contains("?"))
        {
            yield return Path.GetFullPath(Path.Combine(projectDir, include));
            yield break;
        }

        string root = projectDir;
        string pattern = include;
        SearchOption option = SearchOption.TopDirectoryOnly;
        int star = include.IndexOfAny(new[] { '*', '?' });
        int slash = include.LastIndexOf(Path.DirectorySeparatorChar, Math.Max(0, star));

        if (include.Contains("**"))
        {
            int idx = include.IndexOf("**", StringComparison.Ordinal);
            string prefix = include.Substring(0, idx).TrimEnd(Path.DirectorySeparatorChar);
            if (!string.IsNullOrWhiteSpace(prefix))
                root = Path.Combine(projectDir, prefix);
            pattern = Path.GetFileName(include);
            option = SearchOption.AllDirectories;
        }
        else if (slash >= 0)
        {
            root = Path.Combine(projectDir, include.Substring(0, slash));
            pattern = include.Substring(slash + 1);
        }

        if (!Directory.Exists(root))
            yield break;

        foreach (string file in Directory.EnumerateFiles(root, pattern, option))
            yield return Path.GetFullPath(file);
    }
}
